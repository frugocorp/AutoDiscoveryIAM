using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateBulkUserRoles;

public class UpdateBulkUserRolesHandlerTests
{
  private readonly IUpdateBulkUserRolesService _service;
  private readonly UpdateBulkUserRolesHandler _handler;

  public UpdateBulkUserRolesHandlerTests()
  {
    _service = Substitute.For<IUpdateBulkUserRolesService>();
    _handler = new UpdateBulkUserRolesHandler(_service);
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsSuccess_ShouldReturnSameResult()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
    var expected = new UpdateBulkUserRolesResponseDto(
      UserPublicId: userPublicId,
      DryRun: true,
      TotalRequested: 2,
      TotalAssignedNew: 1,
      TotalAssignedReactivated: 0,
      TotalUnassigned: 0,
      TotalUnchanged: 1,
      ExecutedAt: DateTime.UtcNow,
      RequestedPublicIds: rolePublicIds,
      AssignedNewPublicIds: new[] { rolePublicIds[0] },
      AssignedReactivatedPublicIds: Array.Empty<Guid>(),
      UnassignedPublicIds: Array.Empty<Guid>(),
      UnchangedPublicIds: new[] { rolePublicIds[1] });

    _service.ExecuteAsync(userPublicId, rolePublicIds, true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(expected));

    var command = new UpdateBulkUserRolesCommand(userPublicId, rolePublicIds, true);
    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().Be(expected);
    await _service.Received(1).ExecuteAsync(userPublicId, rolePublicIds, true, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenServiceReturnsError_ShouldPropagateError()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicIds = new[] { Guid.NewGuid() };
    const string error = "Usuario inactivo";

    _service.ExecuteAsync(userPublicId, rolePublicIds, false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<UpdateBulkUserRolesResponseDto>(ErrorInfo.Status422Business, error));

    var command = new UpdateBulkUserRolesCommand(userPublicId, rolePublicIds, false);
    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(error);
  }
}
