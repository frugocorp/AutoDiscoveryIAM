// -----------------------------------------------------------------------
// <copyright file="LockoutSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

/// <summary>
/// Configuración de bloqueo de cuenta tras intentos fallidos de login.
/// </summary>
public sealed class LockoutSettings
{
  public int MaxFailedAttempts { get; set; } = 5;
  public int LockoutMinutes { get; set; } = 60;
}
