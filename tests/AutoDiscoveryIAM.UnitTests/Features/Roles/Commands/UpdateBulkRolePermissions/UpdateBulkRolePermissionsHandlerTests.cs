using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.UpdateBulkRolePermissions;

public class UpdateBulkRolePermissionsHandlerTests
{
  private readonly IUpdateBulkRolePermissionsService _service;
  private readonly UpdateBulkRolePermissionsHandler _handler;

  public UpdateBulkRolePermissionsHandlerTests()
  {
    _service = Substitute.For<IUpdateBulkRolePermissionsService>();
    _handler = new UpdateBulkRolePermissionsHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
    var expected = new UpdateBulkRolePermissionsResponseDto(
      RolePublicId: rolePublicId,
      DryRun: true,
      TotalRequested: 2,
      TotalAssignedNew: 1,
      TotalAssignedReactivated: 0,
      TotalUnassigned: 0,
      TotalUnchanged: 1,
      ExecutedAt: DateTime.UtcNow,
      RequestedPublicIds: permissionPublicIds,
      AssignedNewPublicIds: new[] { permissionPublicIds[0] },
      AssignedReactivatedPublicIds: Array.Empty<Guid>(),
      UnassignedPublicIds: Array.Empty<Guid>(),
      UnchangedPublicIds: new[] { permissionPublicIds[1] });

    _service.ExecuteAsync(rolePublicId, permissionPublicIds, true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var command = new UpdateBulkRolePermissionsCommand(rolePublicId, permissionPublicIds, true);
    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(rolePublicId, permissionPublicIds, true, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicIds = new[] { Guid.NewGuid() };
    const string error = "Rol inactivo";

    _service.ExecuteAsync(rolePublicId, permissionPublicIds, false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<UpdateBulkRolePermissionsResponseDto>(ErrorInfo.Status422Business, error));

    var command = new UpdateBulkRolePermissionsCommand(rolePublicId, permissionPublicIds, false);
    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(error);
  }
}
