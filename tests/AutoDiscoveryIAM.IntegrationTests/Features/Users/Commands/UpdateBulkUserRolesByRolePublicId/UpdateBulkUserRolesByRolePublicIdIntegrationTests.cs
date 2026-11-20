using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

[Collection("GlobalTestSetup")]
public class UpdateBulkUserRolesByRolePublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UpdateBulkUserRolesByRolePublicId_WithValidData_ShouldReturnOkAndAssigned()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var rolePublicId = Guid.NewGuid();
        var targetUserPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"test-role-{Guid.NewGuid():N}",
                Description = "role para integration test",
                IsActive = true,
                IsDeleted = false,
            };

            var user = new User
            {
                PublicId = targetUserPublicId,
                Username = $"assign-user-{Guid.NewGuid():N}",
                Email = $"assign-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            };

            dbContext.Roles.Add(role);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsJsonAsync(
          $"/roles/{rolePublicId}/users",
          new UpdateBulkUserRolesByRolePublicIdRequestDto([targetUserPublicId]),
          CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<UpdateBulkUserRolesByRolePublicIdResponse>(cancellationToken: CancellationToken.None);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.RolePublicId.Should().Be(rolePublicId);
        apiResponse.Data.AssignedNewPublicIds.Should().Contain(targetUserPublicId);
    }

    [Fact]
    public async Task UpdateBulkUserRolesByRolePublicId_WithDryRun_ShouldReturnOkAndNotPersist()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var rolePublicId = Guid.NewGuid();
        var targetUserPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"test-role-dryrun-{Guid.NewGuid():N}",
                Description = "role para integration test dry-run",
                IsActive = true,
                IsDeleted = false,
            });

            dbContext.Users.Add(new User
            {
                PublicId = targetUserPublicId,
                Username = $"dryrun-user-{Guid.NewGuid():N}",
                Email = $"dryrun-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsJsonAsync(
          $"/roles/{rolePublicId}/users",
          new UpdateBulkUserRolesByRolePublicIdRequestDto([targetUserPublicId], DryRun: true),
          CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResponse = await response.Content.ReadFromJsonAsync<UpdateBulkUserRolesByRolePublicIdResponse>(cancellationToken: CancellationToken.None);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.DryRun.Should().BeTrue();

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var relationCount = await verifyDbContext.UserRoles
          .CountAsync(relation => relation.Role.PublicId == rolePublicId && relation.User.PublicId == targetUserPublicId, CancellationToken.None);
        relationCount.Should().Be(0);
    }

    private static async Task EnsureDevelopmentPrivilegesAsync(CustomWebApplicationFactory<Program> factory, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

        var devUser = await dbContext.Users.FirstAsync(u => u.Email == "dev@example.com", cancellationToken);
        devUser.IsDevelopment = true;
        devUser.IsActive = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}


