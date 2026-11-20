using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Queries.GetRoles;

[Collection("GlobalTestSetup")]
public class GetRolesIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetRoles_WithSuAdmin_ShouldReturnRolesOrderedByCreatedAtDesc()
  {
    var olderId = Guid.NewGuid();
    var newerId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      dbContext.Roles.AddRange(
          new Role
          {
            PublicId = olderId,
            Name = $"role-old-{Guid.NewGuid():N}",
            Description = "old role",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
          },
          new Role
          {
            PublicId = newerId,
            Name = $"role-new-{Guid.NewGuid():N}",
            Description = "new role",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
          });

      await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync("/roles?pageSize=100", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<GetRolesResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();

    var returned = payload.Data!.Items
        .Where(r => r.PublicId == olderId || r.PublicId == newerId)
        .Select(r => r.PublicId)
        .ToList();

    returned.SequenceEqual(new[] { newerId, olderId }).Should().BeTrue();
  }
}
