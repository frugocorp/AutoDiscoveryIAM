using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.UpdateBulkUserRoles;

[Collection("GlobalTestSetup")]
public class UpdateBulkUserRolesIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UpdateBulkUserRoles_WithValidData_ShouldReturnOkAndAssigned()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var userPublicId = Guid.NewGuid();
        var targetRolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Users.Add(new User
            {
                PublicId = userPublicId,
                Username = $"test-user-{Guid.NewGuid():N}",
                Email = $"test-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            });

            dbContext.Roles.Add(new Role
            {
                PublicId = targetRolePublicId,
                Name = $"target-role-{Guid.NewGuid():N}",
                Description = "role para user bulk integration",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsJsonAsync(
          $"/users/{userPublicId}/roles",
          new UpdateBulkUserRolesRequestDto([targetRolePublicId]),
          CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<UpdateBulkUserRolesResponse>(cancellationToken: CancellationToken.None);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.UserPublicId.Should().Be(userPublicId);
        apiResponse.Data.AssignedNewPublicIds.Should().Contain(targetRolePublicId);
    }

    [Fact]
    public async Task UpdateBulkUserRoles_WithDryRun_ShouldReturnOkAndNotPersist()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var userPublicId = Guid.NewGuid();
        var targetRolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Users.Add(new User
            {
                PublicId = userPublicId,
                Username = $"dryrun-user-{Guid.NewGuid():N}",
                Email = $"dryrun-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            });

            dbContext.Roles.Add(new Role
            {
                PublicId = targetRolePublicId,
                Name = $"dryrun-role-{Guid.NewGuid():N}",
                Description = "dryrun role",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsJsonAsync(
          $"/users/{userPublicId}/roles",
          new UpdateBulkUserRolesRequestDto([targetRolePublicId], DryRun: true),
          CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiResponse = await response.Content.ReadFromJsonAsync<UpdateBulkUserRolesResponse>(cancellationToken: CancellationToken.None);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.DryRun.Should().BeTrue();

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var relationCount = await verifyDbContext.UserRoles
          .CountAsync(relation => relation.User.PublicId == userPublicId && relation.Role.PublicId == targetRolePublicId, CancellationToken.None);
        relationCount.Should().Be(0);
    }

    private static async Task EnsureDevelopmentPrivilegesAsync(CustomWebApplicationFactory<Program> factory, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

        var devUser = await dbContext.Users.FirstAsync(user => user.Email == "dev@example.com", cancellationToken);
        devUser.IsDevelopment = true;
        devUser.IsActive = true;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}


