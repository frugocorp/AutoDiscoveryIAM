// -----------------------------------------------------------------------
// <copyright file="PermissionSyncFromCodeIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Infrastructure.Composition;

/// <summary>
/// <c>app.SyncAutoDiscoveryIAMPermissionsAsync()</c> en una app construida pero todavía sin arrancar
/// (<c>app.Run()</c>), como en un pipeline de despliegue o una bandera de arranque. Usa su propia app
/// y su propia base InMemory: no toca la base compartida de las demás pruebas.
/// </summary>
public class PermissionSyncFromCodeIntegrationTests
{
  private static WebApplication BuildApp()
  {
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["JwtSettings:Key"] = "ClaveDePruebaDelSyncDesdeCodigoConAlMenos32BytesDeLargo",
      ["JwtSettings:Issuer"] = "sync-from-code",
      ["JwtSettings:Audience"] = "sync-from-code",
    });
    var databaseName = $"sync-from-code-{Guid.NewGuid():N}";
    builder.Services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options
      .UseInMemoryDatabase(databaseName)
      .ConfigureWarnings(w => w.Ignore(new Microsoft.Extensions.Logging.EventId(20401))));
    builder.Services.AddAutoDiscoveryIAM(builder.Configuration);
    return builder.Build();
  }

  [Fact(DisplayName = "Sync desde código antes de app.Run(): crea los permisos de la librería y reporta los endpoints propios sin convención")]
  public async Task Sync_BeforeRun_PersistsLibraryPermissions()
  {
    await using var app = BuildApp();
    app.MapAutoDiscoveryIAMEndpoints();
    // Un endpoint propio fuera de la convención Features: el sync lo reporta en vez de descartarlo.
    app.MapGet("/consumer-probe", () => "ok");

    var result = await app.SyncAutoDiscoveryIAMPermissionsAsync(cancellationToken: CancellationToken.None);

    result.OrchestrationResult.PermissionsCreated.Should().BeGreaterThan(0);
    result.OrchestrationResult.EndpointsWithoutPermission.Should().NotBeNull();
    result.OrchestrationResult.EndpointsWithoutPermission!.Any(e => e.Contains("/consumer-probe", StringComparison.Ordinal)).Should().BeTrue();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    (await db.Permissions.AnyAsync(p => p.Key == "users.create_user_by_admin", CancellationToken.None)).Should().BeTrue();
  }

  [Fact(DisplayName = "Sync desde código con DryRun no escribe nada")]
  public async Task Sync_DryRun_DoesNotPersist()
  {
    await using var app = BuildApp();
    app.MapAutoDiscoveryIAMEndpoints();

    var result = await app.SyncAutoDiscoveryIAMPermissionsAsync(new SyncPermissionsRequestDto(DryRun: true), CancellationToken.None);

    result.OrchestrationResult.DryRunMode.Should().BeTrue();
    using var scope = app.Services.CreateScope();
    (await scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>().Permissions.AnyAsync(CancellationToken.None)).Should().BeFalse();
  }

  [Fact(DisplayName = "Sync desde código antes de mapear los endpoints falla con un mensaje claro")]
  public async Task Sync_BeforeMappingEndpoints_Throws()
  {
    await using var app = BuildApp();

    InvalidOperationException? error = null;
    try
    {
      await app.SyncAutoDiscoveryIAMPermissionsAsync(cancellationToken: CancellationToken.None);
    }
    catch (InvalidOperationException ex)
    {
      error = ex;
    }

    error.Should().NotBeNull();
    error!.Message.Contains("MapAutoDiscoveryIAMEndpoints", StringComparison.Ordinal).Should().BeTrue();
  }
}
