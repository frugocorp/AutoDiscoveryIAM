using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;

public interface IActivationService
{
    Task<OperationResult<ActivationStatusDto>> ConfirmByTokenAsync(string token, CancellationToken cancellationToken);
    Task<OperationResult<ActivationStatusDto>> ResendPublicAsync(string email, CancellationToken cancellationToken);
    Task<OperationResult<ActivationStatusDto>> ApproveByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
    Task<OperationResult<ActivationStatusDto>> RejectByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
    Task<OperationResult<ActivationStatusDto>> ResendAdminByPublicIdAsync(Guid publicId, CancellationToken cancellationToken);
    Task<OperationResult<bool>> SendTestEmailAsync(string? subject, string? body, CancellationToken cancellationToken, bool fakeMail = false);

    /// <summary>
    /// Asigna un token de activación nuevo (en la base de datos queda solo su hash) y reinicia la
    /// ventana de reenvíos.
    /// </summary>
    /// <returns>El token en claro, para el enlace del correo. No se puede recuperar después.</returns>
    string AssignActivationToken(User user);
}

public sealed class ActivationService(
  IActivationRepository repository,
  IActivationTokenService tokenService,
  IActivationEmailService emailService,
  IAutoDiscoveryIAMUnitOfWork unitOfWork,
  IOptions<ActivationSettings> activationOptions,
  IConfiguration configuration,
  ILogger<ActivationService> logger,
  IAuthContextService authContextService) : IActivationService
{
    private readonly IAuthContextService _authContextService = authContextService;
    private readonly IActivationRepository _repository = repository;
    private readonly IActivationTokenService _tokenService = tokenService;
    private readonly IActivationEmailService _emailService = emailService;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;
    private readonly ActivationSettings _activation = activationOptions.Value;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<ActivationService> _logger = logger;

    private static readonly Action<ILogger, string, Exception?> LogPublicResendFailed =
      LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1, nameof(LogPublicResendFailed)),
        "No fue posible reenviar correo de activacion para {Email}.");

    public string AssignActivationToken(User user)
    {
        var now = TimeHelpers.GetCurrentMexicoTime();
        var token = IssueActivationToken(user);
        user.EmailVerificationLastSentAt = now;
        user.EmailVerificationResendWindowStartAt = now;
        user.EmailVerificationResendCount = 1;
        return token;
    }

    public async Task<OperationResult<ActivationStatusDto>> ConfirmByTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status400Validation, "Token de activacion requerido.");
        }

        var user = await _repository.GetByTokenHashAsync(OneTimeTokenHasher.Hash(token), cancellationToken);
        if (user == null)
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status404NotFound, "Token de activacion invalido.");
        }

        if (!IsTokenValid(user, token))
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status409Conflict, "Token de activacion expirado o invalido.");
        }

        user.IsEmailVerified = true;
        user.IsActive = true;
        user.Status = UserStatusCatalog.Active;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiration = null;
        user.EmailVerificationLastSentAt = null;
        user.EmailVerificationResendWindowStartAt = null;
        user.EmailVerificationResendCount = 0;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Best-effort: la activacion ya se guardo, un fallo de correo aqui no debe hacer fallar
        // la respuesta -- el usuario ya esta activo de todas formas.
        await _emailService.SendAccountActivatedEmailAsync(user, cancellationToken, fakeMail: false);

        return OperationResult.Ok(new ActivationStatusDto(
          user.Email,
          true,
          "Cuenta activada correctamente.",
          BuildLoginUrl()));
    }

    /// <summary>
    /// Respuesta neutra: igual exista o no la cuenta, y con una duración mínima
    /// (<see cref="ActivationSettings.MinimumResponseMilliseconds"/>) en todos los casos, para que el
    /// tiempo de respuesta tampoco delate si se envió un correo.
    /// </summary>
    public async Task<OperationResult<ActivationStatusDto>> ResendPublicAsync(string email, CancellationToken cancellationToken)
    {
        var startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        var result = await ResendPublicCoreAsync(email, cancellationToken);
        await NeutralResponseTiming.PadAsync(startTimestamp, _activation.MinimumResponseMilliseconds, cancellationToken);
        return result;
    }

    private async Task<OperationResult<ActivationStatusDto>> ResendPublicCoreAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return OperationResult.Ok(NeutralResendResponse());
        }

        var user = await _repository.GetByEmailAsync(email.Trim(), cancellationToken);
        if (user == null || user.IsDeleted || user.IsActive)
        {
            return OperationResult.Ok(NeutralResendResponse());
        }

        var cooldownError = ValidateResendPolicy(user);
        if (cooldownError != null)
        {
            return OperationResult.Ok(NeutralResendResponse());
        }

        var token = IssueActivationToken(user);
        IncreaseResendWindow(user);

        var sendResult = await _emailService.SendActivationEmailAsync(
            user,
            token,
            cancellationToken,
            fakeMail: false);
        if (!sendResult.Success)
        {
            LogPublicResendFailed(_logger, user.Email, null);
            return OperationResult.Ok(NeutralResendResponse());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok(NeutralResendResponse());
    }

    public async Task<OperationResult<ActivationStatusDto>> ApproveByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByPublicIdAsync(publicId, cancellationToken);
        if (user == null)
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
        }

        // Un administrador por rol no aprueba, rechaza ni reactiva una cuenta de nivel superior.
        if (!await UserAccessHierarchy.CanManageAsync(_authContextService, user))
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status403Forbidden, UserAccessHierarchy.InsufficientLevelMessage);
        }

        user.IsActive = true;
        user.IsEmailVerified = true;
        user.Status = UserStatusCatalog.Active;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiration = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Best-effort, misma razon que en ConfirmByTokenAsync.
        await _emailService.SendAccountActivatedEmailAsync(user, cancellationToken, fakeMail: false);

        return OperationResult.Ok(new ActivationStatusDto(
          user.Email,
          true,
          "Usuario aprobado y activado correctamente.",
          BuildLoginUrl()));
    }

    public async Task<OperationResult<ActivationStatusDto>> RejectByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByPublicIdAsync(publicId, cancellationToken);
        if (user == null)
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
        }

        // Un administrador por rol no aprueba, rechaza ni reactiva una cuenta de nivel superior.
        if (!await UserAccessHierarchy.CanManageAsync(_authContextService, user))
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status403Forbidden, UserAccessHierarchy.InsufficientLevelMessage);
        }

        user.IsActive = false;
        user.Status = UserStatusCatalog.Inactive;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiration = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Ok(new ActivationStatusDto(
          user.Email,
          false,
          "Usuario rechazado correctamente.",
          BuildLoginUrl()));
    }

    public async Task<OperationResult<ActivationStatusDto>> ResendAdminByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByPublicIdAsync(publicId, cancellationToken);
        if (user == null)
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
        }

        // Un administrador por rol no aprueba, rechaza ni reactiva una cuenta de nivel superior.
        if (!await UserAccessHierarchy.CanManageAsync(_authContextService, user))
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status403Forbidden, UserAccessHierarchy.InsufficientLevelMessage);
        }

        if (user.IsActive)
        {
            return OperationResult.Fail<ActivationStatusDto>(ErrorInfo.Status409Conflict, "El usuario ya esta activo.");
        }

        var token = IssueActivationToken(user);
        IncreaseResendWindow(user);

        var sendResult = await _emailService.SendActivationEmailAsync(
            user,
            token,
            cancellationToken,
            fakeMail: false);
        if (!sendResult.Success)
        {
            return OperationResult.Fail<ActivationStatusDto>(
              sendResult.ErrorInfo ?? ErrorInfo.Status424Dependency,
              sendResult.Error ?? "No fue posible enviar correo de activacion.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Ok(new ActivationStatusDto(
          user.Email,
          false,
          "Correo de activacion reenviado correctamente.",
          BuildLoginUrl()));
    }

    public Task<OperationResult<bool>> SendTestEmailAsync(string? subject, string? body, CancellationToken cancellationToken, bool fakeMail = false)
        => _emailService.SendTestEmailAsync(subject, body, cancellationToken, fakeMail);

    /// <summary>
    /// Genera un token nuevo en cada envío: en la base de datos solo queda el hash, así que el token
    /// anterior no se puede reenviar (y su enlace deja de servir).
    /// </summary>
    private string IssueActivationToken(User user)
    {
        var token = _tokenService.GenerateToken();
        user.EmailVerificationToken = OneTimeTokenHasher.Hash(token);
        user.EmailVerificationTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(Math.Max(_activation.EmailVerificationTokenExpiryMinutes, 1));
        return token;
    }

    private string? ValidateResendPolicy(User user)
    {
        var now = TimeHelpers.GetCurrentMexicoTime();

        if (user.EmailVerificationLastSentAt.HasValue)
        {
            var cooldownSeconds = Math.Max(_activation.ActivationResendCooldownSeconds, 1);
            var minNextSend = user.EmailVerificationLastSentAt.Value.AddSeconds(cooldownSeconds);
            if (now < minNextSend)
            {
                return "Cooldown activo para reenvio.";
            }
        }

        if (!user.EmailVerificationResendWindowStartAt.HasValue || now > user.EmailVerificationResendWindowStartAt.Value.AddHours(1))
        {
            user.EmailVerificationResendWindowStartAt = now;
            user.EmailVerificationResendCount = 0;
            return null;
        }

        if (user.EmailVerificationResendCount >= Math.Max(_activation.ActivationResendMaxPerHour, 1))
        {
            return "Se alcanzo el limite de reenvios por hora.";
        }

        return null;
    }

    private static void IncreaseResendWindow(User user)
    {
        var now = TimeHelpers.GetCurrentMexicoTime();

        if (!user.EmailVerificationResendWindowStartAt.HasValue || now > user.EmailVerificationResendWindowStartAt.Value.AddHours(1))
        {
            user.EmailVerificationResendWindowStartAt = now;
            user.EmailVerificationResendCount = 0;
        }

        user.EmailVerificationResendCount++;
        user.EmailVerificationLastSentAt = now;
    }

    private static bool IsTokenValid(User user, string? token)
    {
        if (!OneTimeTokenHasher.Matches(user.EmailVerificationToken, token))
        {
            return false;
        }

        if (!user.EmailVerificationTokenExpiration.HasValue)
        {
            return false;
        }

        return user.EmailVerificationTokenExpiration.Value >= TimeHelpers.GetCurrentMexicoTime();
    }

    private ActivationStatusDto NeutralResendResponse()
      => new(
        string.Empty,
        false,
        "Si el correo existe y requiere activacion, se enviara un nuevo enlace.",
        BuildLoginUrl());

    private string BuildLoginUrl()
    {
        var baseUrl = (_configuration["BaseUrl"] ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "/login";
        }

        return $"{baseUrl}/login";
    }
}
