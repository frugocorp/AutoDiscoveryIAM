using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

[Collection("GlobalTestSetup")]
public class ClearBulkRoleHiddenRelationsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ClearBulkRoleHiddenRelations_WithHiddenRelations_ShouldReturnOkAndPersistSoftDelete()
    {
        var rolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"clear-hidden-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            };

            var inactiveUser = new User
            {
                PublicId = Guid.NewGuid(),
                Username = $"clear-hidden-user-{Guid.NewGuid():N}",
                Email = $"clear-hidden-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                Status = "inactive",
                IsActive = false,
                IsDeleted = true,
            };

            var inactivePermission = new Permission
            {
                PublicId = Guid.NewGuid(),
                Key = $"clear.hidden.permission.{Guid.NewGuid():N}",
                IsActive = false,
                IsDeleted = true,
            };

            dbContext.Roles.Add(role);
            dbContext.Users.Add(inactiveUser);
            dbContext.Permissions.Add(inactivePermission);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = inactiveUser.Id,
                RoleId = role.Id,
                User = inactiveUser,
                Role = role,
                IsDeleted = false,
            });

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = inactivePermission.Id,
                Role = role,
                Permission = inactivePermission,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{rolePublicId}/hidden-relations", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ClearBulkRoleHiddenRelationsResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.RolePublicId.Should().Be(rolePublicId);
        payload.Data.ClearedUserRelations.Should().Be(1);
        payload.Data.ClearedPermissionRelations.Should().Be(1);
        payload.Data.TotalCleared.Should().Be(2);

    }

    [Fact]
    public async Task ClearBulkRoleHiddenRelations_WhenRoleDoesNotExist_ShouldReturnNotFound()
    {
        var rolePublicId = Guid.NewGuid();
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{rolePublicId}/hidden-relations", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var payload = await response.Content.ReadFromJsonAsync<ClearBulkRoleHiddenRelationsResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        var expected = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId);
        payload.Error.Should().Contain(expected);
    }

    [Fact]
    public async Task ClearBulkRoleHiddenRelations_WithEmptyRolePublicId_ShouldReturnBadRequestWithValidationMessage()
    {
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{Guid.Empty}/hidden-relations", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadFromJsonAsync<ClearBulkRoleHiddenRelationsResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Error.Should().Contain(RolePermissionAssignmentValidationTypes.RolePublicIdRequired.Message);
    }
}
