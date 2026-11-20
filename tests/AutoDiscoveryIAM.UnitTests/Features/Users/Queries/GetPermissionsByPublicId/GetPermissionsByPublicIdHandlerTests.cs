using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetPermissionsByPublicId;

public class GetPermissionsByPublicIdHandlerTests
{
  private readonly IGetPermissionsByPublicIdService _service;
  private readonly GetPermissionsByPublicIdHandler _handler;

  public GetPermissionsByPublicIdHandlerTests()
  {
    _service = Substitute.For<IGetPermissionsByPublicIdService>();
    _handler = new GetPermissionsByPublicIdHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var userPublicId = Guid.NewGuid();
    var expected = new[]
    {
      new GetPermissionsByPublicIdResponseDto(Guid.NewGuid(), "users.read", "desc", "Users"),
    };

    _service.ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IEnumerable<GetPermissionsByPublicIdResponseDto>>(expected));

    var result = await _handler.Handle(new GetPermissionsByPublicIdQuery(userPublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(userPublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var userPublicId = Guid.NewGuid();
    const string error = "Usuario inactivo";

    _service.ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<IEnumerable<GetPermissionsByPublicIdResponseDto>>(ErrorInfo.Status422Business, error));

    var result = await _handler.Handle(new GetPermissionsByPublicIdQuery(userPublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(error);
  }
}
