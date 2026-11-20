using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

[Collection("GlobalTestSetup")]
public class DeleteRolePermissionsByPermissionPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DeleteRolePermissionsByPermissionPublicId_WithValidData_ShouldReturnOkAndUnassigned()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"delete-role-permission-role-{Guid.NewGuid():N}",
                Description = "role para delete role permission integration",
                IsActive = true,
                IsDeleted = false,
            };

            var permission = new Permission
            {
                PublicId = permissionPublicId,
                Key = $"roles.delete_permission_{Guid.NewGuid():N}",
                Description = "permission para delete role permission integration",
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
                AssignedAt = DateTime.UtcNow,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{rolePublicId}/permissions/{permissionPublicId}", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DeleteRolePermissionsByPermissionPublicIdResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolePublicId.Should().Be(rolePublicId);
        result.Data.PermissionPublicId.Should().Be(permissionPublicId);
    }

    [Fact]
    public async Task DeleteRolePermissionsByPermissionPublicId_WhenRelationDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"missing-delete-role-permission-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            });

            dbContext.Permissions.Add(new Permission
            {
                PublicId = permissionPublicId,
                Key = $"roles.missing_delete_permission_{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.DeleteAsync($"/roles/{rolePublicId}/permissions/{permissionPublicId}", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<DeleteRolePermissionsByPermissionPublicIdResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        var expectedError = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.ActiveRelationNotFound.Message, rolePublicId, permissionPublicId);
        result.Error.Should().Contain(expectedError);
    }
}


