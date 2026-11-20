using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public class UpdateBulkUserRolesByRolePublicIdHandlerTests
{
  private readonly IUpdateBulkUserRolesByRolePublicIdService _service;
  private readonly UpdateBulkUserRolesByRolePublicIdHandler _handler;

  public UpdateBulkUserRolesByRolePublicIdHandlerTests()
  {
    _service = Substitute.For<IUpdateBulkUserRolesByRolePublicIdService>();
    _handler = new UpdateBulkUserRolesByRolePublicIdHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var rolePublicId = Guid.NewGuid();
    var userPublicIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
    var expected = new UpdateBulkUserRolesByRolePublicIdResponseDto(
      RolePublicId: rolePublicId,
      DryRun: true,
      TotalRequested: 2,
      TotalAssignedNew: 1,
      TotalAssignedReactivated: 0,
      TotalUnassigned: 0,
      TotalUnchanged: 1,
      ExecutedAt: DateTime.UtcNow,
      RequestedPublicIds: userPublicIds,
      AssignedNewPublicIds: new[] { userPublicIds[0] },
      AssignedReactivatedPublicIds: Array.Empty<Guid>(),
      UnassignedPublicIds: Array.Empty<Guid>(),
      UnchangedPublicIds: new[] { userPublicIds[1] });

    _service.ExecuteAsync(rolePublicId, userPublicIds, true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var command = new UpdateBulkUserRolesByRolePublicIdCommand(rolePublicId, userPublicIds, true);
    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(rolePublicId, userPublicIds, true, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var rolePublicId = Guid.NewGuid();
    var userPublicIds = new[] { Guid.NewGuid() };
    const string error = "Rol inactivo";

    _service.ExecuteAsync(rolePublicId, userPublicIds, false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<UpdateBulkUserRolesByRolePublicIdResponseDto>(ErrorInfo.Status422Business, error));

    var command = new UpdateBulkUserRolesByRolePublicIdCommand(rolePublicId, userPublicIds, false);
    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(error);
  }
}
