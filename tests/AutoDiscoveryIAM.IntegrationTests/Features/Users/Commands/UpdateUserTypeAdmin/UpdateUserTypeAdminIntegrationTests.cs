using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.UpdateUserTypeAdmin;

[Collection("GlobalTestSetup")]
public class UpdateUserTypeAdminIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task UpdateUserTypeAdmin_WhenUserExists_ShouldPromoteToAdmin()
  {
    await _client.AuthenticateUserAsync(UserType.SuAdmin, CancellationToken.None);

    var seedUsername = $"a04adm_{Guid.NewGuid():N}";
    var seedEmail = $"{seedUsername}@example.com";
    var publicId = await CreateAndResolveUserPublicIdAsync(seedUsername, seedEmail);

    var body = new UpdateUserTypeAdminBodyDto(true, "a04 promote");
    var response = await _client.PutAsJsonAsync($"/users/{publicId}/admin-type", body, CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<UpdateUserTypeAdminResponse>(cancellationToken: CancellationToken.None);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.PublicId.Should().Be(publicId);
    payload.Data.IsAdmin.Should().BeTrue();
  }

  private async Task<Guid> CreateAndResolveUserPublicIdAsync(string username, string email)
  {
    var createRequest = new CreateUserRequestDto(
        username,
        email,
        "TestPass123*");

    var createResponse = await _client.PostAsJsonAsync("/users", createRequest, CancellationToken.None);
    createResponse.EnsureSuccessStatusCode();

    var usersResponse = await _client.GetAsync("/users?pageSize=100", CancellationToken.None);
    usersResponse.EnsureSuccessStatusCode();
    var usersPayload = await usersResponse.Content.ReadFromJsonAsync<GetUsersResponse>(cancellationToken: CancellationToken.None);

    return usersPayload!.Data!.Items.Single(u => u.Username == username).PublicId;
  }
}
