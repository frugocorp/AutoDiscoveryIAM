using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Queries.GetUsersByPublicId;

[Collection("GlobalTestSetup")]
public class GetUsersByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetRoleUsersByPublicId_WhenRoleExistsWithUsers_ShouldReturnOkWithData()
  {
    var rolePublicId = Guid.NewGuid();
    var userPublicId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

      var role = new Role
      {
        PublicId = rolePublicId,
        Name = $"b02-role-users-{Guid.NewGuid():N}",
        IsActive = true,
        IsDeleted = false,
      };

      var user = new User
      {
        PublicId = userPublicId,
        Username = $"b02-role-user-{Guid.NewGuid():N}",
        Email = $"b02-role-user-{Guid.NewGuid():N}@test.local",
        PasswordHash = "hash",
        Status = "active",
        IsActive = true,
        IsDeleted = false,
      };

      dbContext.Roles.Add(role);
      dbContext.Users.Add(user);
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

    var response = await _client.GetAsync($"/roles/{rolePublicId}/users", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<GetUsersByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.Should().Contain(u => u.PublicId == userPublicId);
  }

  [Fact]
  public async Task GetRoleUsersByPublicId_WhenRoleDoesNotExist_ShouldReturnNotFound()
  {
    var rolePublicId = Guid.NewGuid();

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/roles/{rolePublicId}/users", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var payload = await response.Content.ReadFromJsonAsync<GetUsersByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    var expected = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId);
    payload.Error.Should().Contain(expected);
  }
}
