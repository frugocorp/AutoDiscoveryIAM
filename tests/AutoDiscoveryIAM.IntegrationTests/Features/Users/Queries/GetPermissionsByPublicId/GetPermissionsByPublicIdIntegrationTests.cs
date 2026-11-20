using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Queries.GetPermissionsByPublicId;

[Collection("GlobalTestSetup")]
public class GetPermissionsByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetUserPermissionsByPublicId_WhenUserIsInactive_ShouldReturnBusinessError()
  {
    var userPublicId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      dbContext.Users.Add(new User
      {
        PublicId = userPublicId,
        Username = $"users-query-inactive-{Guid.NewGuid():N}",
        Email = $"users-query-inactive-{Guid.NewGuid():N}@test.local",
        PasswordHash = "hash",
        Status = "inactive",
        IsActive = false,
        IsDeleted = true,
      });
      await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/users/{userPublicId}/permissions", CancellationToken.None);

    new[] { (int)HttpStatusCode.UnprocessableEntity, (int)HttpStatusCode.Conflict }.Should().Contain((int)response.StatusCode);
    var payload = await response.Content.ReadFromJsonAsync<GetPermissionsByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    var expected = string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId);
    payload.Error.Should().Contain(expected);
  }
}
