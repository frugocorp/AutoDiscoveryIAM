using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Queries.GetPermissionsByPublicId;

[Collection("GlobalTestSetup")]
public class GetPermissionsByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetRolePermissionsByPublicId_WhenRoleExistsWithPermissions_ShouldReturnOkWithData()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

      var role = new Role
      {
        PublicId = rolePublicId,
        Name = $"b02-role-perms-{Guid.NewGuid():N}",
        IsActive = true,
        IsDeleted = false,
      };

      var permission = new Permission
      {
        PublicId = permissionPublicId,
        Key = $"b02.role.perms.{Guid.NewGuid():N}",
        Group = "b02",
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
        Role = role,
        Permission = permission,
        IsDeleted = false,
      });

      await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/roles/{rolePublicId}/permissions", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<GetPermissionsByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.Should().Contain(p => p.PublicId == permissionPublicId);
  }

  [Fact]
  public async Task GetRolePermissionsByPublicId_WhenRoleDoesNotExist_ShouldReturnNotFound()
  {
    var rolePublicId = Guid.NewGuid();

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/roles/{rolePublicId}/permissions", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var payload = await response.Content.ReadFromJsonAsync<GetPermissionsByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    var expected = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId);
    payload.Error.Should().Contain(expected);
  }
}
