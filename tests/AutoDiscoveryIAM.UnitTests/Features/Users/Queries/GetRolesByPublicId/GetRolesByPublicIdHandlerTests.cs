using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetRolesByPublicId;

public class GetRolesByPublicIdHandlerTests
{
  private readonly IGetRolesByPublicIdService _service;
  private readonly GetRolesByPublicIdHandler _handler;

  public GetRolesByPublicIdHandlerTests()
  {
    _service = Substitute.For<IGetRolesByPublicIdService>();
    _handler = new GetRolesByPublicIdHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var userPublicId = Guid.NewGuid();
    var expected = new[]
    {
      new GetRolesByPublicIdResponseDto(Guid.NewGuid(), "Admin", "desc", true, DateTime.UtcNow),
    };

    _service.ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IEnumerable<GetRolesByPublicIdResponseDto>>(expected));

    var result = await _handler.Handle(new GetRolesByPublicIdQuery(userPublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(userPublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var userPublicId = Guid.NewGuid();
    const string error = "Usuario no encontrado";

    _service.ExecuteAsync(userPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<IEnumerable<GetRolesByPublicIdResponseDto>>(ErrorInfo.Status404NotFound, error));

    var result = await _handler.Handle(new GetRolesByPublicIdQuery(userPublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(error);
  }
}
