// -----------------------------------------------------------------------
// <copyright file="LoginHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

/// <summary>
/// Handler para la autenticación real de usuario y generación de tokens JWT.
/// Valida credenciales contra la base de datos y genera JWT con roles reales.
/// </summary>
public class LoginHandler(
  IAuthLogger logger,
  AutoDiscoveryIAMDbContext dbContext,
  IPasswordHasher passwordHasher,
  IRefreshTokenService refreshTokenService,
  IRefreshTokenRepository refreshTokenRepository,
  IJwtClaimsFactory jwtClaimsFactory,
  IAutoDiscoveryIAMUnitOfWork unitOfWork,
  IOptions<LockoutSettings> lockoutSettings,
  ITwoFactorCodeGenerator twoFactorCodeGenerator,
  ITwoFactorEmailService twoFactorEmailService,
  IOptions<TwoFactorSettings> twoFactorSettings,
  ISecurityEventLogger securityEventLogger)
{
  private readonly IAuthLogger _logger = logger;
  private readonly AutoDiscoveryIAMDbContext _dbContext = dbContext;
  private readonly IPasswordHasher _passwordHasher = passwordHasher;
  private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
  private readonly IJwtClaimsFactory _jwtClaimsFactory = jwtClaimsFactory;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;
  private readonly LockoutSettings _lockoutSettings = lockoutSettings.Value;
  private readonly ITwoFactorCodeGenerator _twoFactorCodeGenerator = twoFactorCodeGenerator;
  private readonly ITwoFactorEmailService _twoFactorEmailService = twoFactorEmailService;
  private readonly TwoFactorSettings _twoFactorSettings = twoFactorSettings.Value;
  private readonly ISecurityEventLogger _securityEventLogger = securityEventLogger;

  /// <summary>
  /// Hash Argon2id de un valor fijo, sin relación con ninguna cuenta real: se verifica contra este
  /// hash cuando el email no existe, para que ese camino consuma un costo de CPU comparable al de
  /// un email real con contraseña incorrecta. Sin esto, un atacante puede distinguir "email existe"
  /// de "email no existe" únicamente por el tiempo de respuesta (el camino de "no existe" retorna
  /// casi instantáneo, mientras que verificar Argon2id toma decenas de milisegundos).
  /// </summary>
  private const string DummyPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$4ffRqZsfCwe4m19C3xOJJw$mnsHPz/WWQiFX4TzCwK/rwNLdaeeUkGIgrd0JGfnjWM";

  /// <summary>
  /// Maneja el proceso de login real con validación de base de datos.
  /// </summary>
  /// <param name="command">Comando con credenciales de usuario.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Resultado de la operación con el DTO de respuesta.</returns>
  public async Task<OperationResult<LoginResponseDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
    {
      _logger.LogLoginFailed(command.Email ?? "unknown");
      _securityEventLogger.LogSecurityEvent(SecurityEventType.LoginFailed, command.Email, command.IpAddress, "/auth/login", "Email o contraseña vacíos.");
      return OperationResult.Fail<LoginResponseDto>(
          ErrorInfo.Status401Unauthorized,
          "Credenciales inválidas.");
    }

    var validation = await ValidateCredentialsAsync(command, cancellationToken);
    if (!validation.Success)
    {
      return OperationResult.Fail<LoginResponseDto>(validation.ErrorInfo!, validation.Error!);
    }

    var user = validation.Data!;

    if (user.TwoFactorEnabled)
    {
      // El contador de intentos fallidos NO se reinicia aquí: lo reinicia VerifyTwoFactor al
      // completar el segundo factor. Si se reiniciara con solo la contraseña, quien la conoce podría
      // alternar códigos fallidos y logins nuevos sin llegar nunca al bloqueo.
      var challengeResponse = await BeginTwoFactorChallengeAsync(user, cancellationToken);
      return OperationResult.Ok(challengeResponse);
    }

    await RegisterSuccessfulAttemptAsync(user, cancellationToken);

    var loginResponse = await GenerateJwtTokenAsync(user, command, cancellationToken);

    _logger.LogLoginSuccess(command.Email);
    _logger.LogTokenGenerated(command.Email);

    return OperationResult.Ok(loginResponse);
  }

  private async Task<LoginResponseDto> BeginTwoFactorChallengeAsync(User user, CancellationToken cancellationToken)
  {
    var challenge = await TwoFactorChallengeStarter.BeginAsync(user, _twoFactorCodeGenerator, _twoFactorEmailService, _twoFactorSettings, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    return challenge;
  }

  private async Task<OperationResult<User>> ValidateCredentialsAsync(LoginCommand command, CancellationToken cancellationToken)
  {
    var user = await GetUserWithRolesAsync(command.Email, cancellationToken);

    if (user == null)
    {
      // Verificación dummy: iguala el costo de CPU con el camino de "contraseña incorrecta" (más
      // abajo), para no delatar por tiempo de respuesta si el email existe o no.
      _passwordHasher.VerifyPassword(command.Password, DummyPasswordHash);
      _logger.LogLoginFailed(command.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.LoginFailed, command.Email, command.IpAddress, "/auth/login", "Usuario no encontrado.");
      return OperationResult.Fail<User>(ErrorInfo.Status401Unauthorized, "Credenciales inválidas.");
    }

    var now = TimeHelpers.GetCurrentMexicoTime();
    var isPasswordValid = IsPasswordValid(user, command.Password);

    if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
    {
      _logger.LogUserBlocked(command.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.AccountLockedOut, command.Email, command.IpAddress, "/auth/login", "Intento de login contra cuenta ya bloqueada.");

      // Solo quien da la contraseña correcta se entera del bloqueo: con una incorrecta la respuesta
      // es la misma que para un email inexistente, así que bloquear una cuenta a propósito no sirve
      // para averiguar qué emails están registrados. Durante el bloqueo no se cuentan intentos.
      return isPasswordValid
        ? OperationResult.Fail<User>(
          ErrorInfo.AccountLockedOut,
          "Cuenta bloqueada temporalmente por multiples intentos fallidos. Intenta de nuevo mas tarde o restablece tu contrasena.")
        : OperationResult.Fail<User>(ErrorInfo.Status401Unauthorized, "Credenciales inválidas.");
    }

    if (!isPasswordValid)
    {
      await RegisterFailedAttemptAsync(user, command.IpAddress, cancellationToken);
      _logger.LogLoginFailed(command.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.LoginFailed, command.Email, command.IpAddress, "/auth/login", "Contraseña inválida.");
      return OperationResult.Fail<User>(ErrorInfo.Status401Unauthorized, "Credenciales inválidas.");
    }

    if (!user.IsActive)
    {
      _logger.LogLoginFailed(command.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.LoginFailed, command.Email, command.IpAddress, "/auth/login", "Cuenta pendiente de activación o inactiva.");
      return OperationResult.Fail<User>(
        ErrorInfo.AccountPendingOrInactive,
        "Tu cuenta esta pendiente de activacion o inactiva. Revisa tu correo o solicita reenvio de activacion.");
    }

    return OperationResult.Ok(user);
  }

  private async Task<User?> GetUserWithRolesAsync(string email, CancellationToken cancellationToken)
  {
    return await _dbContext.Users
      .Include(u => u.UserRoles)
      .ThenInclude(ur => ur.Role)
      .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
  }

  private bool IsPasswordValid(User user, string password)
  {
    return !string.IsNullOrWhiteSpace(user.PasswordHash) &&
           _passwordHasher.VerifyPassword(password, user.PasswordHash);
  }

  private async Task RegisterFailedAttemptAsync(User user, string? ipAddress, CancellationToken cancellationToken)
  {
    var lockedOut = await FailedLoginAttemptRecorder.RecordAsync(_dbContext, _unitOfWork, user, _lockoutSettings, cancellationToken);
    if (lockedOut)
    {
      _logger.LogUserBlocked(user.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.AccountLockedOut, user.Email, ipAddress, "/auth/login", $"Cuenta bloqueada tras {_lockoutSettings.MaxFailedAttempts} intentos fallidos.");
    }
  }

  private async Task RegisterSuccessfulAttemptAsync(User user, CancellationToken cancellationToken)
  {
    if (user.FailedLoginAttempts == 0 && user.LockoutEnd == null)
    {
      return;
    }

    user.FailedLoginAttempts = 0;
    user.LockoutEnd = null;
    await _unitOfWork.SaveChangesAsync(cancellationToken);
  }

  private async Task<LoginResponseDto> GenerateJwtTokenAsync(User user, LoginCommand command, CancellationToken cancellationToken)
  {
    var (tokenString, expiresAt) = _jwtClaimsFactory.BuildAccessToken(user);

    // Generar y persistir refresh token real
    var refreshTokenEntity = _refreshTokenService.GenerateRefreshToken(
      user,
      command.IpAddress,
      command.UserAgent);

    await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

    return new LoginResponseDto(tokenString, expiresAt, refreshTokenEntity.PlaintextToken!);
  }
}
