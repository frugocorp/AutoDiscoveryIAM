using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Permissions.Queries.GetRolesByPublicId;

[Collection("GlobalTestSetup")]
public class GetRolesByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetPermissionRolesByPublicId_WithValidData_ShouldReturnAssignedRoles()
  {
    var permissionPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

      var permission = new Permission
      {
        PublicId = permissionPublicId,
        Key = $"permissions.query.roles.{Guid.NewGuid():N}",
        Group = "permissions",
        IsActive = true,
        IsDeleted = false,
      };

      var role = new Role
      {
        PublicId = rolePublicId,
        Name = $"permissions-query-role-{Guid.NewGuid():N}",
        IsActive = true,
        IsDeleted = false,
      };

      dbContext.Permissions.Add(permission);
      dbContext.Roles.Add(role);
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

    var response = await _client.GetAsync($"/permissions/{permissionPublicId}/roles", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<GetRolesByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.Should().Contain(role => role.PublicId == rolePublicId);
  }

  [Fact]
  public async Task GetPermissionRolesByPublicId_WhenPermissionDoesNotExist_ShouldReturnNotFound()
  {
    var permissionPublicId = Guid.NewGuid();
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/permissions/{permissionPublicId}/roles", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var payload = await response.Content.ReadFromJsonAsync<GetRolesByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    var expected = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionNotFound.Message, permissionPublicId);
    payload.Error.Should().Contain(expected);
  }
}
