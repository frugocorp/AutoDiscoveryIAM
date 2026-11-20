using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.DeleteUserRolesByPublicId;

[Collection("GlobalTestSetup")]
public class DeleteUserRolesByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DeleteUserRolesByPublicId_WithValidData_ShouldReturnOkAndUnassigned()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            var user = new User
            {
                PublicId = userPublicId,
                Username = $"delete-user-roles-user-{Guid.NewGuid():N}",
                Email = $"delete-user-roles-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            };

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"delete-user-roles-role-{Guid.NewGuid():N}",
                Description = "role para delete user roles integration",
                IsActive = true,
                IsDeleted = false,
            };

            dbContext.Users.Add(user);
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.DeleteAsync($"/users/{userPublicId}/roles/{rolePublicId}", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DeleteUserRolesByPublicIdResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserPublicId.Should().Be(userPublicId);
        result.Data.RolePublicId.Should().Be(rolePublicId);
    }

    [Fact]
    public async Task DeleteUserRolesByPublicId_WhenRelationDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Users.Add(new User
            {
                PublicId = userPublicId,
                Username = $"missing-delete-user-role-user-{Guid.NewGuid():N}",
                Email = $"missing-delete-user-role-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            });

            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"missing-delete-user-role-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.DeleteAsync($"/users/{userPublicId}/roles/{rolePublicId}", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<DeleteUserRolesByPublicIdResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        var expectedError = string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.ActiveRelationNotFound.Message, userPublicId, rolePublicId);
        result.Error.Should().Contain(expectedError);
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


