using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public class DeleteRolePermissionsByPermissionPublicIdHandlerTests
{
  private readonly IDeleteRolePermissionsByPermissionPublicIdService _service;
  private readonly DeleteRolePermissionsByPermissionPublicIdHandler _handler;

  public DeleteRolePermissionsByPermissionPublicIdHandlerTests()
  {
    _service = Substitute.For<IDeleteRolePermissionsByPermissionPublicIdService>();
    _handler = new DeleteRolePermissionsByPermissionPublicIdHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var expected = new DeleteRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, permissionPublicId, DateTime.UtcNow);

    _service.ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var result = await _handler.Handle(new DeleteRolePermissionsByPermissionPublicIdCommand(rolePublicId, permissionPublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    const string error = "Relacion activa no encontrada";

    _service.ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<DeleteRolePermissionsByPermissionPublicIdResponseDto>(ErrorInfo.Status404NotFound, error));

    var result = await _handler.Handle(new DeleteRolePermissionsByPermissionPublicIdCommand(rolePublicId, permissionPublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(error);
  }
}
