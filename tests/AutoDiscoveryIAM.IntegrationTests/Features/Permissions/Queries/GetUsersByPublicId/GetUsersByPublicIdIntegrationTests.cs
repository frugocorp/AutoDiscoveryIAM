using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Permissions.Queries.GetUsersByPublicId;

[Collection("GlobalTestSetup")]
public class GetUsersByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetPermissionUsersByPublicId_WhenPermissionIsInactive_ShouldReturnBusinessError()
  {
    var permissionPublicId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      dbContext.Permissions.Add(new Permission
      {
        PublicId = permissionPublicId,
        Key = $"permissions.query.inactive.{Guid.NewGuid():N}",
        IsActive = false,
        IsDeleted = true,
      });
      await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/permissions/{permissionPublicId}/users", CancellationToken.None);

    new[] { (int)HttpStatusCode.UnprocessableEntity, (int)HttpStatusCode.Conflict }.Should().Contain((int)response.StatusCode);
    var payload = await response.Content.ReadFromJsonAsync<GetUsersByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    var expected = string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionInactiveOrDeleted.Message, permissionPublicId);
    payload.Error.Should().Contain(expected);
  }
}
