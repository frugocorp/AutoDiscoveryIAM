// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMUpgradeRunner.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Upgrades;

/// <summary>
/// Aplica, una sola vez por base de datos, los pasos de <see cref="AutoDiscoveryIAMUpgradeSteps.All"/>
/// que todavía no figuran en <see cref="AutoDiscoveryIAMUpgradeRecord"/>. Lo invoca
/// <c>SeedAutoDiscoveryIAMDefaultDataAsync()</c> antes de sembrar, así que actualizar el paquete no
/// requiere ningún paso manual extra en la app consumidora.
/// </summary>
internal static class AutoDiscoveryIAMUpgradeRunner
{
  /// <summary>
  /// Aplica los pasos pendientes, en orden. Cada paso se guarda junto con su registro en el mismo
  /// <c>SaveChangesAsync</c>: si falla, no queda marcado como aplicado y se reintenta en el próximo arranque.
  /// </summary>
  /// <param name="dbContext">Contexto de la librería.</param>
  /// <param name="steps">Pasos a evaluar, en orden.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Los identificadores de los pasos aplicados en esta llamada.</returns>
  public static async Task<IReadOnlyList<string>> RunAsync(
      AutoDiscoveryIAMDbContext dbContext,
      IReadOnlyList<AutoDiscoveryIAMUpgradeStep> steps,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(dbContext);
    ArgumentNullException.ThrowIfNull(steps);

    if (steps.Count == 0)
    {
      return [];
    }

    var appliedIds = (await dbContext.Upgrades
      .Select(record => record.Id)
      .ToListAsync(cancellationToken))
      .ToHashSet(StringComparer.Ordinal);

    var appliedNow = new List<string>();
    foreach (var step in steps.Where(step => !appliedIds.Contains(step.Id)))
    {
      await step.ApplyAsync(dbContext, cancellationToken);
      dbContext.Upgrades.Add(new AutoDiscoveryIAMUpgradeRecord { Id = step.Id, AppliedAtUtc = DateTime.UtcNow });
      await dbContext.SaveChangesAsync(cancellationToken);
      appliedNow.Add(step.Id);
    }

    return appliedNow;
  }
}
