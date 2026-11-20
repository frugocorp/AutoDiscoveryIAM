using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.CreateRole;

[Collection("GlobalTestSetup")]
public class CreateRoleIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateRole_WithValidPayload_ShouldReturnCreated()
    {
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var request = new CreateRoleRequestDto($"a03-role-{Guid.NewGuid():N}", "created by a03", true);
        var response = await _client.PostAsJsonAsync("/roles", request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<CreateRoleResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Name.Should().Be(request.Name);
    }
}
