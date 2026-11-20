// -----------------------------------------------------------------------
// <copyright file="EmailChangeRequestConcurrencyIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.ReviewEmailChangeRequest;

/// <summary>
/// Issue #076: valida contra la base de datos real (no InMemory, que no simula la generación
/// automática de <c>rowversion</c> de SQL Server) que <c>EmailChangeRequest.RowVersion</c> actúa
/// como token de concurrencia optimista — dos contextos que cargan la misma solicitud y la
/// modifican por separado, el segundo <c>SaveChanges</c> debe fallar en vez de pisar
/// silenciosamente al primero (ej. un Admin y un SuAdmin revisando/ejecutando a la vez).
/// </summary>
[Collection("GlobalTestSetup")]
public class EmailChangeRequestConcurrencyIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  [Fact]
  public async Task SaveChanges_WhenRequestWasModifiedConcurrently_ShouldThrowConcurrencyException()
  {
    Guid requestPublicId;

    using (var seedScope = factory.Services.CreateScope())
    {
      var seedContext = seedScope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      var user = new User
      {
        Username = $"myprofile-{Guid.NewGuid():N}",
        Email = $"myprofile-{Guid.NewGuid():N}@example.com",
        PasswordHash = "hash",
        IsActive = true,
      };
      seedContext.Users.Add(user);
      await seedContext.SaveChangesAsync(CancellationToken.None);

      var request = new EmailChangeRequest
      {
        UserId = user.Id,
        RequestedEmail = $"new-{Guid.NewGuid():N}@example.com",
        Status = EmailChangeRequestStatusCatalog.Pending,
      };
      seedContext.EmailChangeRequests.Add(request);
      await seedContext.SaveChangesAsync(CancellationToken.None);
      requestPublicId = request.PublicId;
    }

    using var scopeA = factory.Services.CreateScope();
    using var scopeB = factory.Services.CreateScope();
    var contextA = scopeA.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var contextB = scopeB.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    var requestFromA = await contextA.EmailChangeRequests.FirstAsync(r => r.PublicId == requestPublicId, CancellationToken.None);
    var requestFromB = await contextB.EmailChangeRequests.FirstAsync(r => r.PublicId == requestPublicId, CancellationToken.None);

    requestFromA.Status = EmailChangeRequestStatusCatalog.ApprovedForExecution;
    await contextA.SaveChangesAsync(CancellationToken.None);

    requestFromB.Status = EmailChangeRequestStatusCatalog.Rejected;

    Func<Task> act = async () => await contextB.SaveChangesAsync(CancellationToken.None);

    await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
  }
}
