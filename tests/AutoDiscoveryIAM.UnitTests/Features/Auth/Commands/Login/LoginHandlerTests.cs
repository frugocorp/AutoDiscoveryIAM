// -----------------------------------------------------------------------
// <copyright file="LoginHandlerTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.Data;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login;

/// <summary>
/// Pruebas unitarias para el manejador de comandos de login.
/// Valida la autenticación real contra la base de datos con datos externalizados.
/// </summary>
public class LoginHandlerTests : IDisposable
{
  private readonly AutoDiscoveryIAMDbContext _context;
  private readonly PasswordHasher _passwordHasher;
  private readonly IOptions<JwtSettings> _jwtSettings;
  private readonly IJwtClaimsFactory _jwtClaimsFactory;
  private readonly IAuthLogger _authLogger;
  private readonly IRefreshTokenService _refreshTokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
  private readonly IOptions<LockoutSettings> _lockoutSettings;
  private readonly ITwoFactorEmailService _twoFactorEmailService;
  private readonly LoginHandler _handler;
  private bool _disposed = false;

  /// <summary>
  /// Inicializa una nueva instancia de la clase <c>LoginHandlerTests</c>.
  /// Configura el contexto de prueba con base de datos en memoria.
  /// </summary>
  public LoginHandlerTests()
  {
    // Arrange (Organizar) - Configurar base de datos en memoria
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    _context = new AutoDiscoveryIAMDbContext(options);
    _passwordHasher = new PasswordHasher(); // Usar servicio real
    _authLogger = Substitute.For<IAuthLogger>();

    // Configurar JWT settings
    var jwtSettings = new JwtSettings
    {
      Key = "ThisIsAVeryLongSecretKeyForProbandoPurposesWithAtLeast256Bits12345",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpiryMinutes = 60,
    };
    _jwtSettings = Substitute.For<IOptions<JwtSettings>>();
    _jwtSettings.Value.Returns(jwtSettings);
    _jwtClaimsFactory = new JwtClaimsFactory(_jwtSettings);

    // Configurar dependencias de refresh token
    _refreshTokenService = Substitute.For<IRefreshTokenService>();
    _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    _lockoutSettings = Substitute.For<IOptions<LockoutSettings>>();
    _lockoutSettings.Value.Returns(new LockoutSettings());

    // Configurar mock del refresh token service
    _refreshTokenService.GenerateRefreshToken(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(callInfo => new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
      {
        Token = "mock-refresh-token-hash",
        PlaintextToken = "mock-refresh-token-12345",
        UserId = callInfo.Arg<User>().Id,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IpAddress = callInfo.ArgAt<string>(1),
        UserAgent = callInfo.ArgAt<string>(2),
        IsRevoked = false,
      });

    _twoFactorEmailService = Substitute.For<ITwoFactorEmailService>();
    _twoFactorEmailService.SendCodeAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(OperationResult.Ok(true));

    _handler = new LoginHandler(
      _authLogger, _context, _passwordHasher, _refreshTokenService, _refreshTokenRepository,
      _jwtClaimsFactory, _unitOfWork, _lockoutSettings,
      new TwoFactorCodeGenerator(), _twoFactorEmailService, Options.Create(new TwoFactorSettings()),
      Substitute.For<ISecurityEventLogger>());

    // Configurar datos de prueba en la base de datos
    SeedTestData();
  }

  /// <summary>
  /// Prueba que verifica el login exitoso con credenciales válidas.
  /// </summary>
  /// <param name="email">Email del usuario.</param>
  /// <param name="password">Contraseña del usuario.</param>
  /// <param name="description">Descripción del caso de prueba.</param>
  [Theory]
  [MemberData(nameof(LoginTestData.ValidLoginCasesSimple),
      MemberType = typeof(LoginTestData))]
  public async Task Handle_Should_Return_Success_For_Valid_Credentials(
      string email, string password, string description)
  {
    // Arrange (Organizar)
    var request = new LoginCommand(email, password, "192.168.1.100", "Mozilla/5.0 Chrome/118.0");

    // Act (Actuar)
    var result = await _handler.Handle(request, CancellationToken.None);

    // Assert (Afirmar)
    result.Should().NotBeNull($"porque {description}");
    result.Success.Should().BeTrue($"porque {description}");
    result.Data.Should().NotBeNull();
    result.Data!.Token.Should().NotBeNullOrWhiteSpace();
    // Note: LoginResponseDto no tiene User property, solo Token, ExpiresAt, RefreshToken
  }

  /// <summary>
  /// Prueba que verifica el manejo de credenciales inválidas.
  /// </summary>
  /// <param name="email">Email del usuario.</param>
  /// <param name="password">Contraseña del usuario.</param>
  /// <param name="description">Descripción del caso de prueba.</param>
  [Theory]
  [MemberData(nameof(LoginTestData.InvalidLoginCasesSimple),
      MemberType = typeof(LoginTestData))]
  public async Task Handle_Should_Return_Failure_For_Invalid_Credentials(
      string email, string password, string description)
  {
    // Arrange (Organizar)
    var request = new LoginCommand(email, password, "127.0.0.1", "test-user-agent");

    // Act (Actuar)
    var result = await _handler.Handle(request, CancellationToken.None);

    // Assert (Afirmar)
    result.Should().NotBeNull($"porque {description}");
    result.Success.Should().BeFalse($"porque {description}");
    result.ErrorInfo.Should().NotBeNull();
    if (description == "Inactive user attempting login")
    {
      result.ErrorInfo!.Key.Should().Be(ErrorInfo.AccountPendingOrInactive.Key, $"porque {description}");
      result.Error.Should().Be("Tu cuenta esta pendiente de activacion o inactiva. Revisa tu correo o solicita reenvio de activacion.", $"porque {description}");
    }
    else
    {
      result.Error.Should().Be(AuthValidationTypes.InvalidCredentials.Message, $"porque {description}");
    }
  }

  /// <summary>
  /// Prueba que verifica el manejo de casos edge.
  /// </summary>
  /// <param name="email">Email del usuario.</param>
  /// <param name="password">Contraseña del usuario.</param>
  /// <param name="expectedBehavior">Comportamiento esperado.</param>
  /// <param name="description">Descripción del caso de prueba.</param>
  [Theory]
  [MemberData(nameof(LoginTestData.EdgeCasesLoginData),
      MemberType = typeof(LoginTestData))]
  public async Task Handle_Should_Handle_Edge_Cases_Correctly(
      string email, string password, string expectedBehavior, string description)
  {
    // Arrange (Organizar)
    var request = new LoginCommand(email, password, "127.0.0.1", "test-user-agent");

    // Act (Actuar)
    var result = await _handler.Handle(request, CancellationToken.None);

    // Assert (Afirmar)
    result.Should().NotBeNull($"porque {description}");

    if (expectedBehavior == "failure")
    {
      result.Success.Should().BeFalse($"porque {description}");
    }
    else if (expectedBehavior == "exception")
    {
      // Para algunos casos edge, podríamos esperar excepciones manejadas
      result.Success.Should().BeFalse($"porque {description}");
    }
  }

  /// <summary>
  /// Prueba que verifica que el JWT contiene los claims correctos.
  /// </summary>
  [Fact]
  public async Task Handle_Should_Generate_JWT_With_Correct_Claims()
  {
    // Arrange (Organizar)
    const string email = "admin@example.com";
    const string password = "AdminPass123!";

    var request = new LoginCommand(email, password, "127.0.0.1", "test-user-agent");

    // Act (Actuar)
    var result = await _handler.Handle(request, CancellationToken.None);

    // Assert (Afirmar)
    result.Success.Should().BeTrue();
    result.Data!.Token.Should().NotBeNullOrWhiteSpace();

    // Verificar que el token JWT contiene claims básicos
    var jwtHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var token = jwtHandler.ReadJwtToken(result.Data.Token);

    // Verificar que el token contiene el email con el tipo correcto 'email'
    token.Claims.Should().Contain(c => c.Type == "email" && c.Value == email);
    token.Claims.Should().Contain(c => c.Type == "nameid");
    token.Claims.Should().Contain(c => c.Type == "unique_name");
    token.Claims.Should().NotContain(c => c.Type == "Id" || c.Type == "UserId");
  }

  /// <summary>
  /// Prueba que verifica que usuarios inactivos no pueden hacer login.
  /// </summary>
  [Fact]
  public async Task Handle_Should_Reject_Inactive_Users()
  {
    // Arrange (Organizar)
    const string email = "inactive@example.com";
    const string password = "InactivePass123!";

    var request = new LoginCommand(email, password, "127.0.0.1", "test-user-agent");

    // Act (Actuar)
    var result = await _handler.Handle(request, CancellationToken.None);

    // Assert (Afirmar)
    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
    result.ErrorInfo!.Key.Should().Be(ErrorInfo.AccountPendingOrInactive.Key);
    result.Error.Should().Be("Tu cuenta esta pendiente de activacion o inactiva. Revisa tu correo o solicita reenvio de activacion.");
  }

  /// <summary>
  /// Prueba que verifica que la cuenta se bloquea tras alcanzar el numero maximo de intentos fallidos.
  /// </summary>
  [Fact]
  public async Task Handle_Should_Lockout_Account_After_MaxFailedAttempts()
  {
    const string email = "test@example.com";
    var wrongRequest = new LoginCommand(email, "WrongPassword!", "127.0.0.1", "test-user-agent");

    for (var i = 0; i < 5; i++)
    {
      var attemptResult = await _handler.Handle(wrongRequest, CancellationToken.None);
      attemptResult.Success.Should().BeFalse();
    }

    var user = await _context.Users.FirstAsync(u => u.Email == email, CancellationToken.None);
    user.LockoutEnd.Should().NotBeNull();
    user.LockoutEnd!.Value.Should().BeAfter(TimeHelpers.GetCurrentMexicoTime());

    var correctRequest = new LoginCommand(email, "TestPass123!", "127.0.0.1", "test-user-agent");
    var lockedResult = await _handler.Handle(correctRequest, CancellationToken.None);

    lockedResult.Success.Should().BeFalse();
    lockedResult.ErrorInfo.Should().Be(ErrorInfo.AccountLockedOut);
  }

  [Fact(DisplayName = "Cuenta bloqueada con contrasena incorrecta: misma respuesta que un email inexistente")]
  public async Task Handle_LockedAccountWithWrongPassword_DoesNotRevealTheLockout()
  {
    const string email = "test@example.com";
    var wrongRequest = new LoginCommand(email, "WrongPassword!", "127.0.0.1", "test-user-agent");
    for (var i = 0; i < 5; i++)
    {
      await _handler.Handle(wrongRequest, CancellationToken.None);
    }

    var lockedWithWrongPassword = await _handler.Handle(wrongRequest, CancellationToken.None);
    var unknownEmail = await _handler.Handle(new LoginCommand("nadie@example.com", "WrongPassword!", "127.0.0.1", "test-user-agent"), CancellationToken.None);

    lockedWithWrongPassword.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
    lockedWithWrongPassword.Error.Should().Be(unknownEmail.Error);
  }

  /// <summary>
  /// Prueba que verifica que un login exitoso resetea los contadores de intentos fallidos.
  /// </summary>
  [Fact]
  public async Task Handle_Should_Reset_FailedAttempts_After_Successful_Login()
  {
    const string email = "manager@example.com";
    var wrongRequest = new LoginCommand(email, "WrongPassword!", "127.0.0.1", "test-user-agent");

    await _handler.Handle(wrongRequest, CancellationToken.None);
    await _handler.Handle(wrongRequest, CancellationToken.None);

    var userAfterFailures = await _context.Users.FirstAsync(u => u.Email == email, CancellationToken.None);
    userAfterFailures.FailedLoginAttempts.Should().Be(2);

    var correctRequest = new LoginCommand(email, "ManagerPass123!", "127.0.0.1", "test-user-agent");
    var successResult = await _handler.Handle(correctRequest, CancellationToken.None);

    successResult.Success.Should().BeTrue();
    var userAfterSuccess = await _context.Users.FirstAsync(u => u.Email == email, CancellationToken.None);
    userAfterSuccess.FailedLoginAttempts.Should().Be(0);
    userAfterSuccess.LockoutEnd.Should().BeNull();
  }

  /// <summary>
  /// Prueba que verifica que el login con 2FA por email no emite tokens y envia el codigo.
  /// </summary>
  [Fact]
  public async Task Handle_Should_Return_TwoFactorChallenge_When_Email_Method_Enabled()
  {
    const string email = "2fa-email-user@example.com";
    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "2fa-email-user",
      Email = email,
      PasswordHash = _passwordHasher.HashPassword("TwoFactorPass123!"),
      IsActive = true,
      TwoFactorEnabled = true,
      TwoFactorMethod = TwoFactorMethodCatalog.Email,
      UserRoles = new List<UserRole>(),
    };
    _context.Users.Add(user);
    await _context.SaveChangesAsync(CancellationToken.None);

    var request = new LoginCommand(email, "TwoFactorPass123!", "127.0.0.1", "test-user-agent");
    var result = await _handler.Handle(request, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.RequiresTwoFactor.Should().BeTrue();
    result.Data.Token.Should().BeNull();
    result.Data.RefreshToken.Should().BeNull();
    result.Data.TwoFactorChallengeToken.Should().NotBeNullOrWhiteSpace();

    var reloaded = await _context.Users.FirstAsync(u => u.Email == email, CancellationToken.None);
    reloaded.TwoFactorCode.Should().NotBeNullOrWhiteSpace();
    reloaded.TwoFactorChallengeToken.Should().Be(OneTimeTokenHasher.Hash(result.Data.TwoFactorChallengeToken!));

    await _twoFactorEmailService.Received(1).SendCodeAsync(Arg.Any<User>(), reloaded.TwoFactorCode!, Arg.Any<CancellationToken>(), Arg.Any<bool>());
  }

  /// <summary>
  /// Prueba que verifica que el login con 2FA por Authenticator no emite codigo por correo.
  /// </summary>
  [Fact]
  public async Task Handle_Should_Return_TwoFactorChallenge_Without_Email_When_Authenticator_Method_Enabled()
  {
    const string email = "2fa-authenticator-user@example.com";
    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "2fa-authenticator-user",
      Email = email,
      PasswordHash = _passwordHasher.HashPassword("TwoFactorPass123!"),
      IsActive = true,
      TwoFactorEnabled = true,
      TwoFactorMethod = TwoFactorMethodCatalog.Authenticator,
      TwoFactorSecret = "JBSWY3DPEHPK3PXP",
      UserRoles = new List<UserRole>(),
    };
    _context.Users.Add(user);
    await _context.SaveChangesAsync(CancellationToken.None);

    var request = new LoginCommand(email, "TwoFactorPass123!", "127.0.0.1", "test-user-agent");
    var result = await _handler.Handle(request, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.RequiresTwoFactor.Should().BeTrue();
    result.Data.Token.Should().BeNull();

    var reloaded = await _context.Users.FirstAsync(u => u.Email == email, CancellationToken.None);
    reloaded.TwoFactorCode.Should().BeNull();

    await _twoFactorEmailService.DidNotReceive().SendCodeAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
  }

  /// <summary>
  /// Prueba de performance para verificar que el login no sea excesivamente lento.
  /// </summary>
  /// <param name="email">Email del usuario.</param>
  /// <param name="password">Contraseña del usuario.</param>
  /// <param name="maxExpectedMs">Tiempo máximo esperado en milisegundos.</param>
  /// <param name="description">Descripción del caso de prueba.</param>
  [Theory]
  [Trait("Category", "Performance")]
  [MemberData(nameof(LoginTestData.PerformanceLoginCases),
      MemberType = typeof(LoginTestData))]
  public async Task Handle_Should_Complete_Within_Reasonable_Time(
      string email, string password, int maxExpectedMs, string description)
  {
    // Arrange (Organizar)
    var request = new LoginCommand(email, password, "127.0.0.1", "test-user-agent");

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    // Act (Actuar)
    var result = await _handler.Handle(request, CancellationToken.None);

    // Assert (Afirmar)
    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExpectedMs,
        $"porque {description}");
    result.Should().NotBeNull();
  }

  /// <summary>
  /// Configura los datos de prueba en la base de datos en memoria.
  /// Usa los hashes reales de Argon2id que corresponden a las contraseñas de test.
  /// </summary>
  private void SeedTestData()
  {
    // Crear usuarios de prueba que coincidan con los datos de LoginTestData
    var users = new[]
    {
      new User
      {
        PublicId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
        Email = "admin@example.com",
        PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$imBP+dxp6AfePkAmjVfl9A$Ch3Yrq6VGEnwaO2rxwAM/FjzX5bBXrrKPGseqx9O6Ig", // AdminPass123!
        IsActive = true,
        UserRoles = new List<UserRole>(),
      },
      new User
      {
        PublicId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
        Email = "user@example.com",
        PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$EAbZ0dVvIvPEE2cf4RvOBg$aekIoccqSoBj4bPhbY2UmwSJ4nRki6TwOq3nhowvVXc", // UserPass123!
        IsActive = true,
        UserRoles = new List<UserRole>(),
      },
      new User
      {
        PublicId = Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
        Email = "manager@example.com",
        PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$soHhL4p6TRLhfIcGBUDrGg$EPTmiym8wSPb7xe6438Gycc5BXWoa4r4DmzCP3bcdws", // ManagerPass123!
        IsActive = true,
        UserRoles = new List<UserRole>(),
      },
      new User
      {
        PublicId = Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
        Email = "test@example.com",
        PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU", // TestPass123!
        IsActive = true,
        UserRoles = new List<UserRole>(),
      },
      new User
      {
        PublicId = Guid.Parse("550e8400-e29b-41d4-a716-446655440005"),
        Email = "inactive@example.com",
        PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$pcJKmANNkKndjtOc7z8PsQ$Eer7sl49g7uCqoj65jjk8ZsP3qrpAvw8EbSYqeIa6Q4", // InactivePass123!
        IsActive = false,
        UserRoles = new List<UserRole>(),
      },
    };

    _context.Users.AddRange(users);
    _context.SaveChanges();
  }

  /// <summary>
  /// Libera los recursos utilizados por la clase.
  /// </summary>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Libera los recursos gestionados y no gestionados utilizados por la clase.
  /// </summary>
  /// <param name="disposing">
  /// <c>true</c> para liberar tanto recursos gestionados como no gestionados;
  /// <c>false</c> para liberar únicamente recursos no gestionados.
  /// </param>
  protected virtual void Dispose(bool disposing)
  {
    if (!_disposed && disposing)
    {
      _context?.Dispose();
      _disposed = true;
    }
  }
}



