// -----------------------------------------------------------------------
// <copyright file="TotpService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using OtpNet;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Envoltura sobre Otp.NET para el método Authenticator (TOTP, RFC 6238).
/// </summary>
/// <remarks>
/// El secreto se guarda cifrado con ASP.NET Core Data Protection (<see cref="ProtectSecret"/>): quien
/// lea la base de datos no puede generar códigos válidos. Por eso el anillo de claves de Data
/// Protection tiene que persistir entre reinicios y ser el mismo en todas las instancias; si se
/// pierde, los usuarios con Authenticator ya no pueden completar el segundo factor.
/// </remarks>
public interface ITotpService
{
  /// <summary>Genera un secreto aleatorio codificado en Base32 (en claro, para mostrarlo al enrolar).</summary>
  string GenerateSecret();

  /// <summary>
  /// Construye la URI <c>otpauth://totp/...</c> estándar que el cliente usa para armar el QR de enrolamiento.
  /// </summary>
  string BuildProvisioningUri(string accountEmail, string secret);

  /// <summary>Cifra el secreto en claro para guardarlo en la base de datos.</summary>
  string ProtectSecret(string secret);

  /// <summary>
  /// Valida un código de 6 dígitos contra el secreto guardado (cifrado), con tolerancia de 1 paso
  /// (~30s) por desfase de reloj. Rechaza un código cuyo paso de tiempo no sea posterior a
  /// <paramref name="lastUsedTimeStep"/>: un código ya usado (o uno anterior) no vuelve a servir.
  /// </summary>
  /// <returns>El paso de tiempo del código aceptado, que hay que guardar; <c>null</c> si no es válido.</returns>
  long? ValidateCode(string? protectedSecret, string code, long? lastUsedTimeStep);
}

public sealed partial class TotpService(
  IOptions<TwoFactorSettings> settings,
  IDataProtectionProvider dataProtectionProvider,
  ILogger<TotpService> logger) : ITotpService
{
  private const string ProtectorPurpose = "FrugoCorp.AspNetCore.AutoDiscoveryIAM.TwoFactorSecret.v1";

  private readonly TwoFactorSettings _settings = settings.Value;
  private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
  private readonly ILogger<TotpService> _logger = logger;

  public string GenerateSecret()
  {
    var key = KeyGeneration.GenerateRandomKey(20);
    return Base32Encoding.ToString(key);
  }

  public string BuildProvisioningUri(string accountEmail, string secret)
  {
    var issuer = Uri.EscapeDataString(_settings.Issuer);
    var label = Uri.EscapeDataString($"{_settings.Issuer}:{accountEmail}");
    var secretParam = Uri.EscapeDataString(secret);
    return $"otpauth://totp/{label}?secret={secretParam}&issuer={issuer}&digits=6&period=30";
  }

  public string ProtectSecret(string secret) => _protector.Protect(secret);

  public long? ValidateCode(string? protectedSecret, string code, long? lastUsedTimeStep)
  {
    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(protectedSecret))
    {
      return null;
    }

    string secret;
    try
    {
      secret = _protector.Unprotect(protectedSecret);
    }
    catch (CryptographicException ex)
    {
      LogSecretUnreadable(_logger, ex);
      return null;
    }

    var totp = new Totp(Base32Encoding.ToBytes(secret));
    if (!totp.VerifyTotp(code, out var timeStep, new VerificationWindow(previous: 1, future: 1)))
    {
      return null;
    }

    return lastUsedTimeStep.HasValue && timeStep <= lastUsedTimeStep.Value ? null : timeStep;
  }

  [LoggerMessage(
    EventId = 9101,
    Level = LogLevel.Critical,
    Message = "AutoDiscoveryIAM: no se pudo descifrar un secreto de Authenticator. El anillo de claves de Data Protection cambió o se perdió (persístelo fuera del contenedor y compártelo entre instancias); el usuario no podrá completar el segundo factor hasta que se le desactive (TwoFactorEnabled en false, directo en la base de datos) y vuelva a enrolarse.")]
  private static partial void LogSecretUnreadable(ILogger logger, Exception exception);
}
