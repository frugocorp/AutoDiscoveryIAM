// -----------------------------------------------------------------------
// <copyright file="TwoFactorChallengeStarter.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Inicio del reto de segundo factor, compartido por todo camino que autentica a un usuario con 2FA
/// activo (login con contraseña y login externo): ninguno debe emitir tokens sin pasar por
/// <c>POST /auth/2fa/verify</c>.
/// </summary>
internal static class TwoFactorChallengeStarter
{
  /// <summary>
  /// Asigna el token de reto (y, con 2FA por email, genera y envía el código) sobre
  /// <paramref name="user"/>. No guarda: el llamador persiste los cambios.
  /// </summary>
  /// <returns>La respuesta de login sin tokens, con <c>RequiresTwoFactor</c> y el token de reto.</returns>
  internal static async Task<LoginResponseDto> BeginAsync(
    User user,
    ITwoFactorCodeGenerator codeGenerator,
    ITwoFactorEmailService emailService,
    TwoFactorSettings settings,
    CancellationToken cancellationToken)
  {
    var now = TimeHelpers.GetCurrentMexicoTime();

    // En la base de datos queda solo el hash: quien la lea ve el código de 6 dígitos (o lo deduce),
    // pero sin el token de reto en claro no puede completar el segundo factor.
    var challengeToken = codeGenerator.GenerateChallengeToken();
    user.TwoFactorChallengeToken = OneTimeTokenHasher.Hash(challengeToken);
    user.TwoFactorChallengeTokenExpiration = now.AddMinutes(settings.ChallengeTokenExpiryMinutes);

    if (user.TwoFactorMethod == TwoFactorMethodCatalog.Email)
    {
      user.TwoFactorCode = codeGenerator.GenerateNumericCode(settings.CodeLength);
      user.TwoFactorCodeExpiration = now.AddMinutes(settings.CodeExpiryMinutes);
      await emailService.SendCodeAsync(user, user.TwoFactorCode, cancellationToken);
    }

    return new LoginResponseDto(null, null, null, RequiresTwoFactor: true, TwoFactorChallengeToken: challengeToken);
  }
}
