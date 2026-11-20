using System.Net;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.RbacSecurity;

[Collection("GlobalTestSetup")]
public class SpecialUsersAccessTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AnonymousUser_ShouldReceive401_OnProtectedEndpoint()
    {
        await _client.AuthenticateUserAsync(UserType.Anonymous, CancellationToken.None);

        var response = await _client.GetAsync("/roles", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegularUser_ShouldReceive403_OnSuAdminEndpoint()
    {
        await _client.AuthenticateUserAsync(UserType.Regular, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{Guid.NewGuid()}/hidden-relations", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminUser_ShouldReceive403_OnSuAdminEndpoint()
    {
        await _client.AuthenticateUserAsync(UserType.Admin, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{Guid.NewGuid()}/hidden-relations", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuAdminUser_ShouldPassAuthorization_OnSuAdminEndpoint()
    {
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{Guid.NewGuid()}/hidden-relations", CancellationToken.None);

        // Id inexistente: pasa la autorización y el handler responde 404.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DevelopmentUser_ShouldPassDevelopmentAuthorizationGate()
    {
        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", CancellationToken.None);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminUser_ShouldBypassNormalPermissionChecks()
    {
        await _client.AuthenticateUserAsync(UserType.Admin, CancellationToken.None);

        // /permissions no tiene atributo granular: prueba el bypass general de IsAdmin sobre RBAC.
        var response = await _client.GetAsync("/permissions", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
