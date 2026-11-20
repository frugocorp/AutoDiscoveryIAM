using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Queries;

[Collection("GlobalTestSetup")]
public class RolesQueriesIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetRoleUsersByPublicId_WithValidData_ShouldReturnAssignedUsers()
    {
        var rolePublicId = Guid.NewGuid();
        var userPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"roles-query-user-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            };

            var user = new User
            {
                PublicId = userPublicId,
                Username = $"roles-query-user-{Guid.NewGuid():N}",
                Email = $"roles-query-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                Status = "active",
                IsActive = true,
                IsDeleted = false,
            };

            dbContext.Roles.Add(role);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                User = user,
                Role = role,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync($"/roles/{rolePublicId}/users", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetUsersByPublicIdResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Should().Contain(user => user.PublicId == userPublicId);
    }

    [Fact]
    public async Task GetRolePermissionsByPublicId_WithValidData_ShouldReturnAssignedPermissions()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"roles-query-perm-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            };

            var permission = new Permission
            {
                PublicId = permissionPublicId,
                Key = $"roles.query.permissions.{Guid.NewGuid():N}",
                Group = "roles",
                IsActive = true,
                IsDeleted = false,
            };

            dbContext.Roles.Add(role);
            dbContext.Permissions.Add(permission);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                Role = role,
                Permission = permission,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync($"/roles/{rolePublicId}/permissions", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetPermissionsByPublicIdResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Should().Contain(permission => permission.PublicId == permissionPublicId);
    }

    [Fact]
    public async Task GetRolePermissionsByPublicId_WhenRoleDoesNotExist_ShouldReturnNotFound()
    {
        var rolePublicId = Guid.NewGuid();
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync($"/roles/{rolePublicId}/permissions", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var payload = await response.Content.ReadFromJsonAsync<GetPermissionsByPublicIdResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        var expected = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId);
        payload.Error.Should().Contain(expected);
    }

    [Fact]
    public async Task GetRoleUsersByPublicId_WhenRoleIsInactive_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"roles-query-inactive-{Guid.NewGuid():N}",
                IsActive = false,
                IsDeleted = true,
            });
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync($"/roles/{rolePublicId}/users", CancellationToken.None);

        new[] { (int)HttpStatusCode.UnprocessableEntity, (int)HttpStatusCode.Conflict }.Should().Contain((int)response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GetUsersByPublicIdResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        var expected = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId);
        payload.Error.Should().Contain(expected);
    }
}
