using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Commands.CreateRolePermissions;

[Collection("GlobalTestSetup")]
public class CreateRolePermissionsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateRolePermissions_WithValidData_ShouldReturnOkAndAssigned()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"create-role-permission-role-{Guid.NewGuid():N}",
                Description = "role para create role permission integration",
                IsActive = true,
                IsDeleted = false,
            });

            dbContext.Permissions.Add(new Permission
            {
                PublicId = permissionPublicId,
                Key = $"roles.create_permission_{Guid.NewGuid():N}",
                Description = "permission para create role permission integration",
                IsActive = true,
                IsDeleted = false,
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.PutAsync($"/roles/{rolePublicId}/permissions/{permissionPublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateRolePermissionsResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolePublicId.Should().Be(rolePublicId);
        result.Data.PermissionPublicId.Should().Be(permissionPublicId);
    }

    [Fact]
    public async Task CreateRolePermissions_WithEmptyRolePublicId_ShouldReturnBadRequestWithValidationMessage()
    {
        var permissionPublicId = Guid.NewGuid();

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.PutAsync($"/roles/{Guid.Empty}/permissions/{permissionPublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<CreateRolePermissionsResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Contain(RolePermissionAssignmentValidationTypes.RolePublicIdRequired.Message);
    }

    [Fact]
    public async Task CreateRolePermissions_WhenPermissionDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
            dbContext.Roles.Add(new Role
            {
                PublicId = rolePublicId,
                Name = $"missing-permission-role-{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            });
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

        var response = await _client.PutAsync($"/roles/{rolePublicId}/permissions/{permissionPublicId}", content: null, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<CreateRolePermissionsResponse>(cancellationToken: CancellationToken.None);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        var expectedError = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionNotFound.Message, permissionPublicId);
        result.Error.Should().Contain(expectedError);
    }
}


