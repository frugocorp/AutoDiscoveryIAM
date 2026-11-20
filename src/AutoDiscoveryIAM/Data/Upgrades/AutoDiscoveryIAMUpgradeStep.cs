// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMUpgradeStep.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Upgrades;

/// <summary>
/// Un cambio de DATOS que una versión nueva del paquete necesita aplicar sobre una base ya existente
/// (el esquema lo cubren las migraciones que genera la app consumidora). Ejemplos: renombrar una clave
/// de permiso conservando sus asignaciones, o completar una columna nueva a partir de otra.
/// </summary>
/// <remarks>
/// Reglas para quien agrega un paso a <see cref="AutoDiscoveryIAMUpgradeSteps.All"/>:
/// <list type="bullet">
///   <item><description><see cref="Id"/> es permanente: nunca se cambia ni se reutiliza.</description></item>
///   <item><description>Debe ser seguro en una base recién creada (ahí normalmente no hace nada) y
///   tolerar correr de nuevo si una ejecución anterior se interrumpió.</description></item>
///   <item><description>Solo usa el <see cref="AutoDiscoveryIAMDbContext"/> recibido; el ejecutor guarda los cambios.</description></item>
/// </list>
/// </remarks>
internal abstract class AutoDiscoveryIAMUpgradeStep
{
  /// <summary>
  /// Gets el identificador estable, con la versión que lo introduce como prefijo (ej. <c>0.2.0-...</c>).
  /// </summary>
  public abstract string Id { get; }

  /// <summary>
  /// Aplica el cambio de datos. No llama a <c>SaveChangesAsync</c>: lo hace el ejecutor junto con el registro.
  /// </summary>
  /// <param name="dbContext">Contexto de la librería.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Tarea de la operación.</returns>
  public abstract Task ApplyAsync(AutoDiscoveryIAMDbContext dbContext, CancellationToken cancellationToken);
}

/// <summary>
/// Lista ordenada de pasos de actualización de datos del paquete. Se agregan al final, nunca se reordenan.
/// </summary>
internal static class AutoDiscoveryIAMUpgradeSteps
{
  /// <summary>
  /// Gets todos los pasos, en el orden en que deben aplicarse. Vacía en la primera versión publicada.
  /// </summary>
  public static IReadOnlyList<AutoDiscoveryIAMUpgradeStep> All { get; } = [];
}
