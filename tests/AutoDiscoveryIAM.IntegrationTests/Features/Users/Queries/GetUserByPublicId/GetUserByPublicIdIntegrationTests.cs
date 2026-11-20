using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Queries.GetUserByPublicId;

[Collection("GlobalTestSetup")]
public class GetUserByPublicIdIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetUserByPublicId_WithExistingUser_ShouldReturnOk()
  {
    var userPublicId = Guid.NewGuid();

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      dbContext.Users.Add(new User
      {
        PublicId = userPublicId,
        Username = $"query-user-{Guid.NewGuid():N}",
        Email = $"{Guid.NewGuid():N}@example.com",
        PasswordHash = "hash",
        IsActive = true,
        IsDeleted = false,
      });
      await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/users/{userPublicId}", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<GetUserByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.PublicId.Should().Be(userPublicId);
  }

  [Fact]
  public async Task GetUserByPublicId_WhenUserDoesNotExist_ShouldReturnNotFound()
  {
    var userPublicId = Guid.NewGuid();
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var response = await _client.GetAsync($"/users/{userPublicId}", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    var payload = await response.Content.ReadFromJsonAsync<GetUserByPublicIdResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    payload.Error.Should().Contain(userPublicId.ToString());
  }
}
