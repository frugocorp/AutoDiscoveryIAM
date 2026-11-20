using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.UpdateRoleByPublicId;

[Collection("GlobalTestSetup")]
public class UpdateRoleByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task UpdateRoleByPublicId_WhenRoleExists_ShouldReturnUpdatedData()
  {
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var create = await _client.PostAsJsonAsync(
        "/roles",
        new CreateRoleRequestDto($"a03-upd-{Guid.NewGuid():N}", "for update", true),
        CancellationToken.None);
    create.EnsureSuccessStatusCode();
    var created = await create.Content.ReadFromJsonAsync<CreateRoleResponse>(cancellationToken: CancellationToken.None);

    var updatedName = $"a03-updated-{Guid.NewGuid():N}";
    var body = new UpdateRoleByPublicIdBodyDto(updatedName, "updated description");
    var response = await _client.PutAsJsonAsync($"/roles/{created!.Data!.PublicId}", body, CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<UpdateRoleByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.PublicId.Should().Be(created.Data.PublicId);
    payload.Data.Name.Should().Be(updatedName);
  }
}
