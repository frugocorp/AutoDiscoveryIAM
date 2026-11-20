// -----------------------------------------------------------------------
// <copyright file="NeutralResponseTiming.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

/// <summary>
/// Duración mínima de las respuestas neutras (restablecimiento de contraseña, reenvío de activación):
/// la respuesta tarda lo mismo si la cuenta existe y se envió un correo que si no existe. Un retardo
/// fijo solo en la rama sin correo no alcanza, porque el envío SMTP real tarda más y varía.
/// </summary>
internal static class NeutralResponseTiming
{
  /// <summary>
  /// Espera lo que falte para que, desde <paramref name="startTimestamp"/>
  /// (<see cref="Stopwatch.GetTimestamp"/>), hayan pasado al menos <paramref name="minimumMilliseconds"/>.
  /// </summary>
  public static Task PadAsync(long startTimestamp, int minimumMilliseconds, CancellationToken cancellationToken)
  {
    var remaining = TimeSpan.FromMilliseconds(Math.Max(minimumMilliseconds, 0)) - Stopwatch.GetElapsedTime(startTimestamp);
    return remaining > TimeSpan.Zero ? Task.Delay(remaining, cancellationToken) : Task.CompletedTask;
  }
}
