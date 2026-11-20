using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Queries.GetUsers;

[Collection("GlobalTestSetup")]
public class GetUsersIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetUsers_WhenAuthenticatedAsSuAdmin_ShouldReturnOkWithList()
    {
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync("/users", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetUsersResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUsers_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        var anonClient = factory.CreateClient();

        var response = await anonClient.GetAsync("/users", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
