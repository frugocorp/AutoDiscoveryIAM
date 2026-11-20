using System.Text;
using System.Text.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetMyPermissions;

public class GetMyPermissionsEndPointTests
{
  private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

  [Fact]
  public async Task HandleGetMyPermissionsAsync_WhenHandlerReturnsSuccess_ShouldReturn200Response()
  {
    var authContext = Substitute.For<IAuthContextService>();
    var service = Substitute.For<IGetMyPermissionsService>();
    var userPublicId = Guid.NewGuid();

    authContext.GetCurrentUserPublicId().Returns(userPublicId);
    service.ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IEnumerable<GetMyPermissionsResponseDto>>(
      [
        new GetMyPermissionsResponseDto(Guid.NewGuid(), "users.read", "Can read users", "Users"),
      ]));

    var handler = new GetMyPermissionsHandler(authContext, service);

    var result = await GetMyPermissionsEndPoint.HandleGetMyPermissionsAsync(handler, CancellationToken.None);
    var (statusCode, body) = await ExecuteResultAsync(result);
    var payload = JsonSerializer.Deserialize<GetMyPermissionsResponse>(body, JsonOptions);

    statusCode.Should().Be(StatusCodes.Status200OK);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data.Should().NotBeNull();
    payload.Data!.Should().HaveCount(1);
    payload.Data!.First().Key.Should().Be("users.read");
  }

  [Fact]
  public async Task HandleGetMyPermissionsAsync_WhenHandlerReturnsFailure_ShouldRespectErrorStatusCode()
  {
    var authContext = Substitute.For<IAuthContextService>();
    var service = Substitute.For<IGetMyPermissionsService>();
    var userPublicId = Guid.NewGuid();

    authContext.GetCurrentUserPublicId().Returns(userPublicId);
    service.ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<IEnumerable<GetMyPermissionsResponseDto>>(ErrorInfo.Status422Business, "Usuario inactivo"));

    var handler = new GetMyPermissionsHandler(authContext, service);

    var result = await GetMyPermissionsEndPoint.HandleGetMyPermissionsAsync(handler, CancellationToken.None);
    var (statusCode, body) = await ExecuteResultAsync(result);
    var payload = JsonSerializer.Deserialize<GetMyPermissionsResponse>(body, JsonOptions);

    statusCode.Should().Be(ErrorInfo.Status422Business.StatusCode);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeFalse();
    payload.Error.Should().Be("Usuario inactivo");
  }

  private static async Task<(int StatusCode, string Body)> ExecuteResultAsync(IResult result)
  {
    var context = new DefaultHttpContext();
    context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
    context.Response.Body = new MemoryStream();

    await result.ExecuteAsync(context);

    context.Response.Body.Seek(0, SeekOrigin.Begin);
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
    var content = await reader.ReadToEndAsync();

    return (context.Response.StatusCode, content);
  }
}
