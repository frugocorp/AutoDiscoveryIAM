using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetUsersByPublicId;

public class GetUsersByPublicIdHandlerTests
{
  private readonly IGetUsersByPublicIdService _service;
  private readonly GetUsersByPublicIdHandler _handler;

  public GetUsersByPublicIdHandlerTests()
  {
    _service = Substitute.For<IGetUsersByPublicIdService>();
    _handler = new GetUsersByPublicIdHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var rolePublicId = Guid.NewGuid();
    var expected = new[]
    {
      new GetUsersByPublicIdResponseDto(Guid.NewGuid(), "jdoe", "jdoe@test.com", DateTime.UtcNow),
    };

    _service.ExecuteAsync(rolePublicId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IEnumerable<GetUsersByPublicIdResponseDto>>(expected));

    var result = await _handler.Handle(new GetUsersByPublicIdQuery(rolePublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(rolePublicId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var rolePublicId = Guid.NewGuid();
    const string error = "Rol no encontrado";

    _service.ExecuteAsync(rolePublicId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<IEnumerable<GetUsersByPublicIdResponseDto>>(ErrorInfo.Status404NotFound, error));

    var result = await _handler.Handle(new GetUsersByPublicIdQuery(rolePublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(error);
  }
}
