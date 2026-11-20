// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdConcurrencyIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// Issue #075: valida contra la base de datos real (no InMemory, que no simula la generación
/// automática de <c>rowversion</c> de SQL Server) que <c>Role.RowVersion</c> realmente actúa como
/// token de concurrencia optimista — dos contextos que cargan la misma fila y la modifican por
/// separado, el segundo <c>SaveChanges</c> debe fallar en vez de pisar silenciosamente el primero.
/// </summary>
[Collection("GlobalTestSetup")]
public class UpdateRoleByPublicIdConcurrencyIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task SaveChanges_WhenRoleWasModifiedConcurrently_ShouldThrowConcurrencyException()
  {
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var create = await _client.PostAsJsonAsync(
        "/roles",
        new CreateRoleRequestDto($"a03-concurrency-{Guid.NewGuid():N}", "for concurrency test", true),
        CancellationToken.None);
    create.EnsureSuccessStatusCode();
    var created = await create.Content.ReadFromJsonAsync<CreateRoleResponse>(cancellationToken: CancellationToken.None);
    var publicId = created!.Data!.PublicId;

    using var scopeA = factory.Services.CreateScope();
    using var scopeB = factory.Services.CreateScope();
    var contextA = scopeA.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var contextB = scopeB.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    var roleFromA = await contextA.Roles.FirstAsync(r => r.PublicId == publicId, CancellationToken.None);
    var roleFromB = await contextB.Roles.FirstAsync(r => r.PublicId == publicId, CancellationToken.None);

    roleFromA.Description = "updated by A";
    await contextA.SaveChangesAsync(CancellationToken.None);

    roleFromB.Description = "updated by B, but stale";

    Func<Task> act = async () => await contextB.SaveChangesAsync(CancellationToken.None);

    await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
  }
}
