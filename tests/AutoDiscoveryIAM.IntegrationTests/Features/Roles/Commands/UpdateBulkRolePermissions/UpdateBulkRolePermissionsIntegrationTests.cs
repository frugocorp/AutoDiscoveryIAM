using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.UpdateBulkRolePermissions;

[Collection("GlobalTestSetup")]
public class UpdateBulkRolePermissionsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UpdateBulkRolePermissions_WithValidData_ShouldReturnOkAndAssigned()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"bulk-role-permissions-role-{Guid.NewGuid():N}",
                Description = "role para bulk permission integration",
                IsActive = true,
                IsDeleted = false,
            });

            dbContext.Permissions.Add(new Permission
            {
                PublicId = permissionPublicId,
                Key = $"roles.bulk_permission_{Guid.NewGuid():N}",
                Description = "permission para bulk role permission integration",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.PutAsJsonAsync(
            $"/roles/{rolePublicId}/permissions",
            new UpdateBulkRolePermissionsRequestDto([permissionPublicId]),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<UpdateBulkRolePermissionsResponse>(cancellationToken: CancellationToken.None);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.RolePublicId.Should().Be(rolePublicId);
        apiResponse.Data.AssignedNewPublicIds.Should().Contain(permissionPublicId);
    }

    [Fact]
    public async Task UpdateBulkRolePermissions_WithDryRun_ShouldReturnOkAndNotPersist()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"bulk-role-permissions-dryrun-role-{Guid.NewGuid():N}",
                Description = "dryrun role para bulk permission integration",
                IsActive = true,
                IsDeleted = false,
            });

            dbContext.Permissions.Add(new Permission
            {
                PublicId = permissionPublicId,
                Key = $"roles.bulk_dryrun_permission_{Guid.NewGuid():N}",
                Description = "dryrun permission para bulk role permission integration",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.PutAsJsonAsync(
            $"/roles/{rolePublicId}/permissions",
            new UpdateBulkRolePermissionsRequestDto([permissionPublicId], DryRun: true),
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResponse = await response.Content.ReadFromJsonAsync<UpdateBulkRolePermissionsResponse>(cancellationToken: CancellationToken.None);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.DryRun.Should().BeTrue();

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var relationCount = await verifyDbContext.RolePermissions
            .CountAsync(relation => relation.Role.PublicId == rolePublicId && relation.Permission.PublicId == permissionPublicId, CancellationToken.None);
        relationCount.Should().Be(0);
    }
}


