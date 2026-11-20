// -----------------------------------------------------------------------
// <copyright file="VerifyTwoFactorCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

/// <summary>
/// Comando para completar el login validando el segundo factor de autenticación.
/// </summary>
/// <param name="ChallengeToken">Token de desafío devuelto por <c>/auth/login</c> cuando el usuario tiene 2FA activo.</param>
/// <param name="Code">Código de un solo uso (email) o código TOTP de 6 dígitos (Authenticator).</param>
/// <param name="IpAddress">Dirección IP del cliente para trazabilidad.</param>
/// <param name="UserAgent">User-Agent del cliente para trazabilidad.</param>
public record VerifyTwoFactorCommand(
  string ChallengeToken,
  string Code,
  string? IpAddress = null,
  string? UserAgent = null);
