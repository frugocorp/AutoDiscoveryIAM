using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Queries.GetRolesByPublicId;

[Collection("GlobalTestSetup")]
public class GetRolesByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetUserRolesByPublicId_WithValidData_ShouldReturnAssignedRoles()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            var user = new User
            {
                PublicId = userPublicId,
                Username = $"users-query-user-{Guid.NewGuid():N}",
                Email = $"users-query-user-{Guid.NewGuid():N}@test.local",
                PasswordHash = "hash",
                Status = "active",
                IsActive = true,
                IsDeleted = false,
            };

            var role = new Role
            {
                PublicId = rolePublicId,
                Name = $"users-query-role-{Guid.NewGuid():N}",
                Description = "role para users query integration",
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
                User = user,
                Role = role,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync($"/users/{userPublicId}/roles", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetRolesByPublicIdResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Should().Contain(role => role.PublicId == rolePublicId);
    }

    [Fact]
    public async Task GetUserRolesByPublicId_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var userPublicId = Guid.NewGuid();
        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.GetAsync($"/users/{userPublicId}/roles", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var payload = await response.Content.ReadFromJsonAsync<GetRolesByPublicIdResponse>(cancellationToken: CancellationToken.None);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        var expected = string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId);
        payload.Error.Should().Contain(expected);
    }
}
