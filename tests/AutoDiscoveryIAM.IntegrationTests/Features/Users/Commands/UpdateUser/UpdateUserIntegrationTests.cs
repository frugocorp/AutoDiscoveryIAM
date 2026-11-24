using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.UpdateUser;

[Collection("GlobalTestSetup")]
public class UpdateUserIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task UpdateUser_WhenUserExists_ShouldReturnUpdatedFields()
  {
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var seedUsername = $"a04usr_{Guid.NewGuid():N}";
    var seedEmail = $"{seedUsername}@example.com";
    var publicId = await CreateAndResolveUserPublicIdAsync(seedUsername, seedEmail);

    var body = new UpdateUserBodyDto($"{seedUsername}_upd", $"upd_{seedUsername}@example.com", "a04 update user");
    var response = await _client.PutAsJsonAsync($"/users/{publicId}", body, CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<UpdateUserResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.PublicId.Should().Be(publicId);
    payload.Data.Username.Should().Be($"{seedUsername}_upd");
  }

  private async Task<Guid> CreateAndResolveUserPublicIdAsync(string username, string email)
  {
    var createRequest = new CreateUserRequestDto(
        username,
        email,
        "TestPass123*");

    var createResponse = await _client.PostAsJsonAsync("/users", createRequest, CancellationToken.None);
    createResponse.EnsureSuccessStatusCode();

    // De la base, no de la primera página de /users: otras pruebas dejan usuarios que la llenan.
    return await TestDataLookup.GetUserPublicIdByUsernameAsync(factory.Services, username);
  }
}
