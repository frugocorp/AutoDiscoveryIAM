using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.CreateUserRoles;

[Collection("GlobalTestSetup")]
public class CreateUserRolesIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateUserRoles_WithValidData_ShouldReturnOkAndAssigned()
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
                Username = $"create-user-roles-user-{Guid.NewGuid():N}",
                Email = $"create-user-roles-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            });

            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"create-user-roles-role-{Guid.NewGuid():N}",
                Description = "role para create user roles integration",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsync($"/users/{userPublicId}/roles/{rolePublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateUserRolesResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserPublicId.Should().Be(userPublicId);
        result.Data.RolePublicId.Should().Be(rolePublicId);
    }

    [Fact]
    public async Task CreateUserRoles_WithEmptyUserPublicId_ShouldReturnBadRequestWithValidationMessage()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var rolePublicId = Guid.NewGuid();
        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsync($"/users/{Guid.Empty}/roles/{rolePublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<CreateUserRolesResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Contain(UserRoleAssignmentValidationTypes.UserPublicIdRequired.Message);
    }

    [Fact]
    public async Task CreateUserRoles_WhenUserDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        await EnsureDevelopmentPrivilegesAsync(factory, CancellationToken.None);

        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"missing-user-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            });
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsync($"/users/{userPublicId}/roles/{rolePublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<CreateUserRolesResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        var expectedError = string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId);
        result.Error.Should().Contain(expectedError);
    }

    [Fact]
    public async Task CreateUserRoles_WhenRelationAlreadyExists_ShouldReturnBusinessErrorWithCentralizedMessage()
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
                Username = $"duplicate-user-role-user-{Guid.NewGuid():N}",
                Email = $"duplicate-user-role-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            };

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"duplicate-user-role-role-{Guid.NewGuid():N}",
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

        var response = await _client.PutAsync($"/users/{userPublicId}/roles/{rolePublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var result = await response.Content.ReadFromJsonAsync<CreateUserRolesResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        var expectedError = string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RelationAlreadyAssigned.Message, userPublicId, rolePublicId);
        result.Error.Should().Contain(expectedError);
    }

    [Fact]
    public async Task CreateUserRoles_WhenRelationIsSoftDeleted_ShouldReactivateRelationAndReturnOk()
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
                Username = $"restore-user-role-user-{Guid.NewGuid():N}",
                Email = $"restore-user-role-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                IsActive = true,
                IsDeleted = false,
                Status = "active",
            };

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"restore-user-role-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            };

            dbContext.Users.Add(user);
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(CancellationToken.None);

            var softDeletedRelation = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow.AddDays(-1),
            };

            dbContext.UserRoles.Add(softDeletedRelation);

            await dbContext.SaveChangesAsync(CancellationToken.None);

            softDeletedRelation.IsDeleted = true;
            softDeletedRelation.DeletedAt = DateTime.UtcNow.AddHours(-1);
            softDeletedRelation.DeletedBy = 1L;

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.Development, CancellationToken.None);

        var response = await _client.PutAsync($"/users/{userPublicId}/roles/{rolePublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateUserRolesResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserPublicId.Should().Be(userPublicId);
        result.Data.RolePublicId.Should().Be(rolePublicId);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var relation = await verifyDbContext.UserRoles
            .IgnoreQueryFilters()
            .FirstAsync(item => item.User.PublicId == userPublicId && item.Role.PublicId == rolePublicId, CancellationToken.None);
        relation.IsDeleted.Should().BeFalse();
        relation.RestoredAt.Should().NotBeNull();
        relation.RestoredBy.Should().NotBeNull();
        relation.RestoredBy.Should().BeGreaterThan(0);
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



