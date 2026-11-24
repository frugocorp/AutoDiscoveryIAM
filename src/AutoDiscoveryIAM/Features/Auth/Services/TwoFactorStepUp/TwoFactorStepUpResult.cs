// -----------------------------------------------------------------------
// <copyright file="TwoFactorStepUpResult.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.TwoFactorStepUp;

/// <summary>
/// Resultado de una verificación step-up con el Authenticator.
/// </summary>
public enum TwoFactorStepUpStatus
{
  /// <summary>
  /// Código válido y no usado antes: la acción sensible puede continuar.
  /// </summary>
  Verified,

  /// <summary>
  /// Código incorrecto, vencido o ya usado. Cuenta como intento fallido.
  /// </summary>
  InvalidCode,

  /// <summary>
  /// La cuenta está bloqueada por intentos fallidos (este intento o uno anterior). Ver
  /// <see cref="TwoFactorStepUpResult.LockoutEnd"/>.
  /// </summary>
  LockedOut,

  /// <summary>
  /// El usuario no tiene el Authenticator activo: no hay contra qué verificar.
  /// </summary>
  NotEnrolled,

  /// <summary>
  /// No hay usuario autenticado, o el usuario no existe o está inactivo.
  /// </summary>
  UserNotFound,
}

/// <summary>
/// Resultado de <see cref="ITwoFactorStepUpService"/>.
/// </summary>
/// <param name="Status">Estado de la verificación.</param>
/// <param name="LockoutEnd">Fin del bloqueo cuando <paramref name="Status"/> es <see cref="TwoFactorStepUpStatus.LockedOut"/>.</param>
public sealed record TwoFactorStepUpResult(TwoFactorStepUpStatus Status, DateTime? LockoutEnd = null)
{
  /// <summary>
  /// Atajo para <see cref="TwoFactorStepUpStatus.Verified"/>.
  /// </summary>
  public bool IsVerified => Status == TwoFactorStepUpStatus.Verified;
}
