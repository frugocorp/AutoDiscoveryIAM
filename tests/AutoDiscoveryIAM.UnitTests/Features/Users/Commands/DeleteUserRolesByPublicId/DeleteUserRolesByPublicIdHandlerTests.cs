using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.DeleteUserRolesByPublicId;

public class DeleteUserRolesByPublicIdHandlerTests
{
  private readonly IDeleteUserRolesByPublicIdService _service;
  private readonly DeleteUserRolesByPublicIdHandler _handler;

  public DeleteUserRolesByPublicIdHandlerTests()
  {
    _service = Substitute.For<IDeleteUserRolesByPublicIdService>();
    _handler = new DeleteUserRolesByPublicIdHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();
    var expected = new DeleteUserRolesByPublicIdResponseDto(userPublicId, rolePublicId, DateTime.UtcNow);

    _service.ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var result = await _handler.Handle(new DeleteUserRolesByPublicIdCommand(userPublicId, rolePublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();
    const string error = "Relacion activa no encontrada";

    _service.ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<DeleteUserRolesByPublicIdResponseDto>(ErrorInfo.Status404NotFound, error));

    var result = await _handler.Handle(new DeleteUserRolesByPublicIdCommand(userPublicId, rolePublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(error);
  }
}
