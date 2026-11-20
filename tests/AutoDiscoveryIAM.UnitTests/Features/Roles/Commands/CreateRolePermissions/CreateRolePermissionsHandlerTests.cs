using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.CreateRolePermissions;

public class CreateRolePermissionsHandlerTests
{
  private readonly ICreateRolePermissionsService _service;
  private readonly CreateRolePermissionsHandler _handler;

  public CreateRolePermissionsHandlerTests()
  {
    _service = Substitute.For<ICreateRolePermissionsService>();
    _handler = new CreateRolePermissionsHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var expected = new CreateRolePermissionsResponseDto(rolePublicId, permissionPublicId, DateTime.UtcNow);

    _service.ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var result = await _handler.Handle(new CreateRolePermissionsCommand(rolePublicId, permissionPublicId), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    const string error = "Relacion ya asignada";

    _service.ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<CreateRolePermissionsResponseDto>(ErrorInfo.Status422Business, error));

    var result = await _handler.Handle(new CreateRolePermissionsCommand(rolePublicId, permissionPublicId), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(error);
  }
}
