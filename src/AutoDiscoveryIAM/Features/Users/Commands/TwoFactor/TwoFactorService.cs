using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;

public interface ITwoFactorService
{
  Task<OperationResult<TwoFactorStatusDto>> EnableEmailAsync(Guid userPublicId, string currentPassword, string? currentCode, CancellationToken cancellationToken);
  Task<OperationResult<EnrollAuthenticatorResponseDto>> EnrollAuthenticatorAsync(Guid userPublicId, string currentPassword, string? currentCode, CancellationToken cancellationToken);
  Task<OperationResult<TwoFactorStatusDto>> ConfirmAuthenticatorAsync(Guid userPublicId, string code, CancellationToken cancellationToken);
  Task<OperationResult<TwoFactorStatusDto>> DisableAsync(Guid userPublicId, string currentPassword, string? currentCode, CancellationToken cancellationToken);
}

public sealed class TwoFactorService(
  ITwoFactorRepository repository,
  IPasswordHasher passwordHasher,
  ITotpService totpService,
  IAutoDiscoveryIAMUnitOfWork unitOfWork) : ITwoFactorService
{
  private readonly ITwoFactorRepository _repository = repository;
  private readonly IPasswordHasher _passwordHasher = passwordHasher;
  private readonly ITotpService _totpService = totpService;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

  /// <summary>Cuenta sin contraseña con Authenticator activo: falta el código actual o no es válido.</summary>
  internal const string CurrentCodeInvalidMessage = "Se requiere un codigo valido de tu Authenticator actual.";

  public async Task<OperationResult<TwoFactorStatusDto>> EnableEmailAsync(Guid userPublicId, string currentPassword, string? currentCode, CancellationToken cancellationToken)
  {
    var user = await _repository.GetByPublicIdAsync(userPublicId, cancellationToken);
    if (user == null)
    {
      return OperationResult.Fail<TwoFactorStatusDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
    }

    var passwordError = ValidateReauthentication(user, currentPassword, currentCode);
    if (passwordError != null)
    {
      return OperationResult.Fail<TwoFactorStatusDto>(ErrorInfo.Status401Unauthorized, passwordError);
    }

    user.TwoFactorMethod = TwoFactorMethodCatalog.Email;
    user.TwoFactorEnabled = true;
    user.TwoFactorPendingSecret = null;

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return OperationResult.Ok(new TwoFactorStatusDto(user.TwoFactorMethod, true, "Verificacion en dos pasos por correo activada."));
  }

  public async Task<OperationResult<EnrollAuthenticatorResponseDto>> EnrollAuthenticatorAsync(Guid userPublicId, string currentPassword, string? currentCode, CancellationToken cancellationToken)
  {
    var user = await _repository.GetByPublicIdAsync(userPublicId, cancellationToken);
    if (user == null)
    {
      return OperationResult.Fail<EnrollAuthenticatorResponseDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
    }

    var passwordError = ValidateReauthentication(user, currentPassword, currentCode);
    if (passwordError != null)
    {
      return OperationResult.Fail<EnrollAuthenticatorResponseDto>(ErrorInfo.Status401Unauthorized, passwordError);
    }

    // Se muestra en claro una sola vez (para el QR); en la base de datos queda cifrado.
    var secret = _totpService.GenerateSecret();
    user.TwoFactorPendingSecret = _totpService.ProtectSecret(secret);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    var provisioningUri = _totpService.BuildProvisioningUri(user.Email, secret);
    return OperationResult.Ok(new EnrollAuthenticatorResponseDto(secret, provisioningUri));
  }

  public async Task<OperationResult<TwoFactorStatusDto>> ConfirmAuthenticatorAsync(Guid userPublicId, string code, CancellationToken cancellationToken)
  {
    var user = await _repository.GetByPublicIdAsync(userPublicId, cancellationToken);
    if (user == null)
    {
      return OperationResult.Fail<TwoFactorStatusDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
    }

    if (string.IsNullOrWhiteSpace(user.TwoFactorPendingSecret))
    {
      return OperationResult.Fail<TwoFactorStatusDto>(ErrorInfo.Status409Conflict, "No hay un enrolamiento de Authenticator pendiente.");
    }

    var timeStep = _totpService.ValidateCode(user.TwoFactorPendingSecret, code, lastUsedTimeStep: null);
    if (timeStep is null)
    {
      return OperationResult.Fail<TwoFactorStatusDto>(ErrorInfo.Status401Unauthorized, "Codigo de verificacion invalido.");
    }

    // El código usado para confirmar el enrolamiento tampoco sirve después para iniciar sesión.
    user.TwoFactorLastUsedTimeStep = timeStep;
    user.TwoFactorSecret = user.TwoFactorPendingSecret;
    user.TwoFactorPendingSecret = null;
    user.TwoFactorMethod = TwoFactorMethodCatalog.Authenticator;
    user.TwoFactorEnabled = true;

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return OperationResult.Ok(new TwoFactorStatusDto(user.TwoFactorMethod, true, "Authenticator confirmado y activado."));
  }

  public async Task<OperationResult<TwoFactorStatusDto>> DisableAsync(Guid userPublicId, string currentPassword, string? currentCode, CancellationToken cancellationToken)
  {
    var user = await _repository.GetByPublicIdAsync(userPublicId, cancellationToken);
    if (user == null)
    {
      return OperationResult.Fail<TwoFactorStatusDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
    }

    var passwordError = ValidateReauthentication(user, currentPassword, currentCode);
    if (passwordError != null)
    {
      return OperationResult.Fail<TwoFactorStatusDto>(ErrorInfo.Status401Unauthorized, passwordError);
    }

    ClearTwoFactorState(user);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return OperationResult.Ok(new TwoFactorStatusDto(TwoFactorMethodCatalog.None, false, "Verificacion en dos pasos desactivada."));
  }

  /// <summary>
  /// Confirma que quien cambia el 2FA es el dueño de la cuenta: con su contraseña actual o, en una
  /// cuenta sin contraseña (solo login social) que ya tiene Authenticator, con un código válido de
  /// ese Authenticator. Sin esto, un access token robado bastaría para apagarlo o llevarlo a otro
  /// dispositivo.
  /// </summary>
  private string? ValidateReauthentication(User user, string currentPassword, string? currentCode)
  {
    if (!string.IsNullOrWhiteSpace(user.PasswordHash))
    {
      return string.IsNullOrWhiteSpace(currentPassword) || !_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash)
        ? UserValidationTypes.CurrentPasswordInvalid.Message
        : null;
    }

    if (user.TwoFactorEnabled && user.TwoFactorMethod == TwoFactorMethodCatalog.Authenticator)
    {
      var timeStep = _totpService.ValidateCode(user.TwoFactorSecret, currentCode ?? string.Empty, user.TwoFactorLastUsedTimeStep);
      if (timeStep is null)
      {
        return CurrentCodeInvalidMessage;
      }

      user.TwoFactorLastUsedTimeStep = timeStep;
    }

    return null;
  }

  private static void ClearTwoFactorState(User user)
  {
    user.TwoFactorEnabled = false;
    user.TwoFactorMethod = TwoFactorMethodCatalog.None;
    user.TwoFactorSecret = null;
    user.TwoFactorPendingSecret = null;
    user.TwoFactorLastUsedTimeStep = null;
    user.TwoFactorCode = null;
    user.TwoFactorCodeExpiration = null;
    user.TwoFactorChallengeToken = null;
    user.TwoFactorChallengeTokenExpiration = null;
  }
}
