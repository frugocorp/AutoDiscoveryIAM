using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.DeleteRoleByPublicId;

[Collection("GlobalTestSetup")]
public class DeleteRoleByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task DeleteRoleByPublicId_WhenRoleExists_ShouldReturnOk()
  {
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var create = await _client.PostAsJsonAsync(
        "/roles",
        new CreateRoleRequestDto($"a03-del-{Guid.NewGuid():N}", "for delete", true),
        CancellationToken.None);
    create.EnsureSuccessStatusCode();
    var created = await create.Content.ReadFromJsonAsync<CreateRoleResponse>(cancellationToken: CancellationToken.None);

    var response = await _client.DeleteAsync($"/roles/{created!.Data!.PublicId}", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<DeleteRoleByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.PublicId.Should().Be(created.Data.PublicId);
  }
}
