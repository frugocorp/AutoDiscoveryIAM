using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetMyPermissions;

public class GetMyPermissionsHandlerTests
{
  private readonly IAuthContextService _authContextService;
  private readonly IGetMyPermissionsService _service;
  private readonly GetMyPermissionsHandler _handler;

  public GetMyPermissionsHandlerTests()
  {
    _authContextService = Substitute.For<IAuthContextService>();
    _service = Substitute.For<IGetMyPermissionsService>();
    _handler = new GetMyPermissionsHandler(_authContextService, _service);
  }

  [Fact]
  public async Task Handle_WhenCurrentUserPublicIdIsMissing_ShouldDelegateWithNullUserId()
  {
    _authContextService.GetCurrentUserPublicId().Returns((Guid?)null);
    _service.ExecuteAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<IEnumerable<GetMyPermissionsResponseDto>>(ErrorInfo.Status401Unauthorized, "context invalid"));

    var result = await _handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
    result.Error.Should().Be("context invalid");
    await _service.Received(1).ExecuteAsync((Guid?)null, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenCurrentUserIsResolved_ShouldDelegateToService()
  {
    var userPublicId = Guid.NewGuid();
    var expected = new[]
    {
      new GetMyPermissionsResponseDto(Guid.NewGuid(), "users.read", "desc", "Users"),
    };

    _authContextService.GetCurrentUserPublicId().Returns(userPublicId);
    _service
      .ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IEnumerable<GetMyPermissionsResponseDto>>(expected));

    var result = await _handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(userPublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateResult()
  {
    var userPublicId = Guid.NewGuid();

    _authContextService.GetCurrentUserPublicId().Returns(userPublicId);
    _service
      .ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<IEnumerable<GetMyPermissionsResponseDto>>(ErrorInfo.Status422Business, "Usuario inactivo"));

    var result = await _handler.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be("Usuario inactivo");
  }
}
