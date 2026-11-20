using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Queries.GetMyPermissions;

[Collection("GlobalTestSetup")]
public class GetMyPermissionsIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string UserPassword = "UserPass123!";
    private const string UserPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$EAbZ0dVvIvPEE2cf4RvOBg$aekIoccqSoBj4bPhbY2UmwSJ4nRki6TwOq3nhowvVXc";

    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    [Fact]
    public async Task GetMyPermissions_WhenNoToken_ShouldReturnUnauthorized()
    {
        _client.RemoveAuthentication();

        var response = await _client.GetAsync("/users/me/permissions", _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyPermissions_WhenAuthenticatedUserHasNoRoles_ShouldReturnEmptyList()
    {
        var email = $"get-my-perms-no-roles-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(email);
        await AuthenticateAsync(email);

        var response = await _client.GetAsync("/users/me/permissions", _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetMyPermissionsResponse>(cancellationToken: _cancellationToken);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyPermissions_WhenAuthenticatedUserHasRolePermissions_ShouldReturnDistinctPermissions()
    {
        var email = $"get-my-perms-with-roles-{Guid.NewGuid():N}@example.com";
        var key = $"users.read_me_permissions_{Guid.NewGuid():N}";

        await CreateUserWithPermissionAsync(email, key);
        await AuthenticateAsync(email);

        var response = await _client.GetAsync("/users/me/permissions", _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetMyPermissionsResponse>(cancellationToken: _cancellationToken);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Should().Contain(permission => permission.Key == key);
    }

    private async Task CreateUserAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

        var user = new User
        {
            PublicId = Guid.NewGuid(),
            Username = $"get-my-perms-{Guid.NewGuid():N}",
            Email = email,
            PasswordHash = UserPasswordHash,
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(_cancellationToken);
    }

    private async Task CreateUserWithPermissionAsync(string email, string permissionKey)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

        var user = new User
        {
            PublicId = Guid.NewGuid(),
            Username = $"get-my-perms-user-{Guid.NewGuid():N}",
            Email = email,
            PasswordHash = UserPasswordHash,
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var role = new Role
        {
            PublicId = Guid.NewGuid(),
            Name = $"get-my-perms-role-{Guid.NewGuid():N}",
            Description = "Role for get my permissions integration tests",
            IsActive = true,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            PublicId = Guid.NewGuid(),
            Key = permissionKey,
            Description = "Permission for get my permissions integration tests",
            Group = "Users",
            IsActive = true,
            IsDeleted = false,
        };

        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync(_cancellationToken);

        dbContext.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role,
            IsDeleted = false,
        });

        dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            Role = role,
            Permission = permission,
            IsDeleted = false,
        });

        await dbContext.SaveChangesAsync(_cancellationToken);
    }

    private async Task AuthenticateAsync(string email)
    {
        var token = await _client.GetAuthTokenAsync(email, UserPassword, _cancellationToken);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}