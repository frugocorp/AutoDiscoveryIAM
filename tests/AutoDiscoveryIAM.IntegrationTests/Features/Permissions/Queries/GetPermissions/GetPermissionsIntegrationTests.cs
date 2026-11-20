using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Permissions.Queries.GetPermissions;

[Collection("GlobalTestSetup")]
public class GetPermissionsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetPermissions_WithAuthenticatedUser_ShouldReturnOrderedPermissions()
  {
    var lowKey = $"perm.a.{Guid.NewGuid():N}";
    var highKey = $"perm.b.{Guid.NewGuid():N}";

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      dbContext.Permissions.AddRange(
          new Permission
          {
            PublicId = Guid.NewGuid(),
            Key = highKey,
            Group = "BGroup",
            Description = "second",
            IsActive = true,
            IsDeleted = false,
          },
          new Permission
          {
            PublicId = Guid.NewGuid(),
            Key = lowKey,
            Group = "AGroup",
            Description = "first",
            IsActive = true,
            IsDeleted = false,
          });

      await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync("/permissions?pageSize=100", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<GetPermissionsResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();

    var returned = payload.Data!.Items
        .Where(p => p.Key == lowKey || p.Key == highKey)
        .Select(p => p.Key)
        .ToList();

    returned.SequenceEqual(new[] { lowKey, highKey }).Should().BeTrue();
  }
}
