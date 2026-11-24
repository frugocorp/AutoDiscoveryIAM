// -----------------------------------------------------------------------
// <copyright file="LoginResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

/// <summary>
/// DTO para la respuesta de login. Cuando el usuario tiene 2FA activo, <paramref name="RequiresTwoFactor"/>
/// es <c>true</c> y <paramref name="Token"/>/<paramref name="ExpiresAt"/>/<paramref name="RefreshToken"/> quedan
/// nulos hasta que se verifique el segundo factor vía <c>POST /auth/2fa/verify</c> con <paramref name="TwoFactorChallengeToken"/>.
/// <paramref name="PasswordChangeRequired"/> en <c>true</c> indica que la contraseña la definió un administrador: la sesión
/// solo sirve para los endpoints propios de la cuenta hasta cambiarla en <c>POST /users/me/password</c>.
/// </summary>
public record LoginResponseDto(
  string? Token,
  DateTime? ExpiresAt,
  string? RefreshToken,
  bool RequiresTwoFactor = false,
  string? TwoFactorChallengeToken = null,
  bool PasswordChangeRequired = false);
