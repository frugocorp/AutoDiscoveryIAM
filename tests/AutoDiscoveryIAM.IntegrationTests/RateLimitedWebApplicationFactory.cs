// -----------------------------------------------------------------------
// <copyright file="RateLimitedWebApplicationFactory.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests;

/// <summary>
/// Igual que <see cref="CustomWebApplicationFactory{T}"/>, pero conserva los límites por defecto de
/// la librería en sus rutas sensibles, para probarlos tal como los recibe un consumidor.
/// </summary>
public class RateLimitedWebApplicationFactory : CustomWebApplicationFactory<Program>
{
  /// <inheritdoc/>
  protected override bool EnforceLibraryRateLimits => true;
}
