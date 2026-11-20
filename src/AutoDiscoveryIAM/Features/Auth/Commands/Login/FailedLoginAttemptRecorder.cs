// -----------------------------------------------------------------------
// <copyright file="FailedLoginAttemptRecorder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

/// <summary>
/// Cuenta un intento fallido (contraseña o segundo factor) y bloquea la cuenta al llegar al máximo.
/// </summary>
/// <remarks>
/// Intentos simultáneos sobre la misma cuenta chocan en el <c>RowVersion</c> del usuario. Sin el
/// reintento, solo uno de ellos quedaría contado y un atacante podría probar contraseñas en paralelo
/// sin llegar nunca al bloqueo: cada conflicto recarga el usuario y vuelve a sumar el intento.
/// </remarks>
internal static class FailedLoginAttemptRecorder
{
  private const int MaxSaveAttempts = 10;

  /// <summary>Registra el intento fallido y guarda.</summary>
  /// <returns><c>true</c> si este intento dejó la cuenta bloqueada.</returns>
  internal static async Task<bool> RecordAsync(
    AutoDiscoveryIAMDbContext dbContext,
    IAutoDiscoveryIAMUnitOfWork unitOfWork,
    User user,
    LockoutSettings settings,
    CancellationToken cancellationToken)
  {
    for (var attempt = 1; ; attempt++)
    {
      user.FailedLoginAttempts++;
      var lockedOut = user.FailedLoginAttempts >= settings.MaxFailedAttempts;
      if (lockedOut)
      {
        user.LockoutEnd = TimeHelpers.GetCurrentMexicoTime().AddMinutes(settings.LockoutMinutes);
        user.FailedLoginAttempts = 0;
      }

      try
      {
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return lockedOut;
      }
      catch (DbUpdateConcurrencyException) when (attempt < MaxSaveAttempts)
      {
        await dbContext.Entry(user).ReloadAsync(cancellationToken);
      }
    }
  }
}
