using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.RestoreRoleByPublicId;

[Collection("GlobalTestSetup")]
public class RestoreRoleByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task RestoreRoleByPublicId_WhenRoleWasDeleted_ShouldReturnOk()
    {
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var create = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleRequestDto($"a03-res-{Guid.NewGuid():N}", "for restore", true),
            CancellationToken.None);
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreateRoleResponse>(cancellationToken: CancellationToken.None);

        var delete = await _client.DeleteAsync($"/roles/{created!.Data!.PublicId}", CancellationToken.None);
        delete.EnsureSuccessStatusCode();

        var response = await _client.PostAsync($"/roles/{created.Data.PublicId}/restore", null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<RestoreRoleByPublicIdResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.PublicId.Should().Be(created.Data.PublicId);
    }
}
