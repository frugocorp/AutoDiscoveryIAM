using System.Net;
using System.Text.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.RbacSecurity;

[Collection("GlobalTestSetup")]
public class ApiSecurityTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetUsers_ResponseShouldNotExposeInternalId()
    {
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync("/users", CancellationToken.None);
        var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Contains("\"id\":", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        payload.Contains("\"publicId\":", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Fact]
    public async Task GetRoles_ResponseShouldNotExposeInternalId()
    {
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync("/roles", CancellationToken.None);
        var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Contains("\"id\":", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        payload.Contains("\"publicId\":", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Fact]
    public async Task GetPermissions_ResponseShouldNotExposeInternalId()
    {
        await _client.AuthenticateUserAsync(UserType.Admin, CancellationToken.None);

        var response = await _client.GetAsync("/permissions", CancellationToken.None);
        var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Contains("\"id\":", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        payload.Contains("\"publicId\":", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Fact]
    public async Task ForbiddenResponse_ShouldNotLeakInternalMetadata()
    {
        await _client.AuthenticateUserAsync(UserType.Regular, CancellationToken.None);

        var response = await _client.GetAsync("/roles", CancellationToken.None);
        var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);
        var parsed = JsonSerializer.Deserialize<ApiResponse<object>>(payload, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        parsed.Should().NotBeNull();
        parsed!.ErrorInfo.Should().NotBeNull();
        payload.Contains("stackTrace", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        payload.Contains("namespace", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        payload.Contains("at FrugoCorp.AspNetCore.AutoDiscoveryIAM", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }

    [Fact]
    public async Task RegularUser_ShouldNotReadOtherUserRoles()
    {
        await _client.AuthenticateUserAsync(UserType.Regular, CancellationToken.None);

        var randomTargetUser = Guid.NewGuid();
        var response = await _client.GetAsync($"/users/{randomTargetUser}/roles", CancellationToken.None);

        (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.NotFound).Should().BeTrue();
    }

    [Trait("Category", "Security")]
    [Fact(DisplayName = "IDOR: un usuario Regular no puede leer los permisos de otro usuario por su publicId")]
    public async Task RegularUser_ShouldNotReadOtherUserPermissions()
    {
        await _client.AuthenticateUserAsync(UserType.Regular, CancellationToken.None);

        var randomTargetUser = Guid.NewGuid();
        var response = await _client.GetAsync($"/users/{randomTargetUser}/permissions", CancellationToken.None);

        (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.NotFound).Should().BeTrue();
    }

    [Trait("Category", "Security")]
    [Fact(DisplayName = "IDOR: un usuario Regular no puede leer el perfil de otro usuario por su publicId")]
    public async Task RegularUser_ShouldNotReadOtherUserByPublicId()
    {
        await _client.AuthenticateUserAsync(UserType.Regular, CancellationToken.None);

        var randomTargetUser = Guid.NewGuid();
        var response = await _client.GetAsync($"/users/{randomTargetUser}", CancellationToken.None);

        (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.NotFound).Should().BeTrue();
    }
}
