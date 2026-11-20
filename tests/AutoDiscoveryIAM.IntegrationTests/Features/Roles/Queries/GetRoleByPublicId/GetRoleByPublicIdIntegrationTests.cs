using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Queries.GetRoleByPublicId;

[Collection("GlobalTestSetup")]
public class GetRoleByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetRoleByPublicId_WithExistingRole_ShouldReturnOk()
  {
    var rolePublicId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      dbContext.Roles.Add(new Role
      {
        PublicId = rolePublicId,
        Name = $"query-role-{Guid.NewGuid():N}",
        Description = "role by public id",
        IsActive = true,
        IsDeleted = false,
      });
      await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/roles/{rolePublicId}", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<GetRoleByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.PublicId.Should().Be(rolePublicId);
  }

  [Fact]
  public async Task GetRoleByPublicId_WhenRoleDoesNotExist_ShouldReturnNotFound()
  {
    var rolePublicId = Guid.NewGuid();
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/roles/{rolePublicId}", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var payload = await response.Content.ReadFromJsonAsync<GetRoleByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    payload.Error.Should().Contain(rolePublicId.ToString());
  }
}
