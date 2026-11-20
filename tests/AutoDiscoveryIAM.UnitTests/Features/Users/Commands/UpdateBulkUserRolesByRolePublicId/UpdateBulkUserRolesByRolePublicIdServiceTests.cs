using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public class UpdateBulkUserRolesByRolePublicIdServiceTests
{
  private readonly IRoleUserAssignmentService _assignmentService;
  private readonly UpdateBulkUserRolesByRolePublicIdService _service;

  public UpdateBulkUserRolesByRolePublicIdServiceTests()
  {
    _assignmentService = Substitute.For<IRoleUserAssignmentService>();
    _service = new UpdateBulkUserRolesByRolePublicIdService(_assignmentService);
  }

  [Fact]
  public async Task ExecuteAsync_WhenAssignmentServiceFails_ShouldReturnSameError()
  {
    var rolePublicId = Guid.NewGuid();
    var expectedError = string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.ActiveRoleNotFound.Message, rolePublicId);
    _assignmentService
      .UpdateRoleUsersAsync(rolePublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<RoleUserAssignmentBulkResult>(ErrorInfo.Status404NotFound, expectedError));

    var result = await _service.ExecuteAsync(rolePublicId, [Guid.NewGuid()], dryRun: true, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(expectedError);
  }

  [Fact]
  public async Task ExecuteAsync_WhenAssignmentServiceSucceeds_ShouldMapAllScalarFields()
  {
    var rolePublicId = Guid.NewGuid();
    var userPublicId = Guid.NewGuid();
    var executedAt = DateTime.UtcNow;

    var bulkResult = new RoleUserAssignmentBulkResult(
      OwnerPublicId: rolePublicId,
      DryRun: true,
      TotalRequested: 4,
      TotalAssignedNew: 1,
      TotalAssignedReactivated: 1,
      TotalUnassigned: 1,
      TotalUnchanged: 1,
      ExecutedAt: executedAt,
      RequestedPublicIds: [userPublicId],
      AssignedNewPublicIds: [userPublicId],
      AssignedReactivatedPublicIds: [],
      UnassignedPublicIds: [],
      UnchangedPublicIds: []);

    _assignmentService
      .UpdateRoleUsersAsync(rolePublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(bulkResult));

    var result = await _service.ExecuteAsync(rolePublicId, [userPublicId], dryRun: true, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.RolePublicId.Should().Be(rolePublicId);
    result.Data.DryRun.Should().BeTrue();
    result.Data.TotalRequested.Should().Be(4);
    result.Data.TotalAssignedNew.Should().Be(1);
    result.Data.TotalAssignedReactivated.Should().Be(1);
    result.Data.TotalUnassigned.Should().Be(1);
    result.Data.TotalUnchanged.Should().Be(1);
    result.Data.ExecutedAt.Should().Be(executedAt);
    result.Data.AssignedNewPublicIds.Should().ContainSingle().Which.Should().Be(userPublicId);
  }

  [Fact]
  public async Task ExecuteAsync_WhenAssignmentServiceSucceeds_ShouldMapAllCollections()
  {
    var rolePublicId = Guid.NewGuid();
    var requestedA = Guid.NewGuid();
    var requestedB = Guid.NewGuid();
    var assignedNew = Guid.NewGuid();
    var reactivated = Guid.NewGuid();
    var unassigned = Guid.NewGuid();
    var unchanged = Guid.NewGuid();

    var bulkResult = new RoleUserAssignmentBulkResult(
      OwnerPublicId: rolePublicId,
      DryRun: false,
      TotalRequested: 2,
      TotalAssignedNew: 1,
      TotalAssignedReactivated: 1,
      TotalUnassigned: 1,
      TotalUnchanged: 1,
      ExecutedAt: DateTime.UtcNow,
      RequestedPublicIds: [requestedA, requestedB],
      AssignedNewPublicIds: [assignedNew],
      AssignedReactivatedPublicIds: [reactivated],
      UnassignedPublicIds: [unassigned],
      UnchangedPublicIds: [unchanged]);

    _assignmentService
      .UpdateRoleUsersAsync(rolePublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(bulkResult));

    var result = await _service.ExecuteAsync(rolePublicId, [requestedA, requestedB], dryRun: false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.RequestedPublicIds.Should().BeEquivalentTo(new[] { requestedA, requestedB });
    result.Data.AssignedNewPublicIds.Should().BeEquivalentTo(new[] { assignedNew });
    result.Data.AssignedReactivatedPublicIds.Should().BeEquivalentTo(new[] { reactivated });
    result.Data.UnassignedPublicIds.Should().BeEquivalentTo(new[] { unassigned });
    result.Data.UnchangedPublicIds.Should().BeEquivalentTo(new[] { unchanged });
  }

  [Fact]
  public async Task ExecuteAsync_ShouldForwardArgumentsToAssignmentService()
  {
    var rolePublicId = Guid.NewGuid();
    var userA = Guid.NewGuid();
    var userB = Guid.NewGuid();
    var cancellationTokenSource = new CancellationTokenSource();
    var cancellationToken = cancellationTokenSource.Token;
    var requested = new[] { userA, userB };

    _assignmentService
      .UpdateRoleUsersAsync(rolePublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(new RoleUserAssignmentBulkResult(
        OwnerPublicId: rolePublicId,
        DryRun: true,
        TotalRequested: 2,
        TotalAssignedNew: 0,
        TotalAssignedReactivated: 0,
        TotalUnassigned: 0,
        TotalUnchanged: 2,
        ExecutedAt: DateTime.UtcNow,
        RequestedPublicIds: requested,
        AssignedNewPublicIds: [],
        AssignedReactivatedPublicIds: [],
        UnassignedPublicIds: [],
        UnchangedPublicIds: requested)));

    var result = await _service.ExecuteAsync(rolePublicId, requested, dryRun: true, cancellationToken);

    result.Success.Should().BeTrue();
    await _assignmentService.Received(1).UpdateRoleUsersAsync(
      rolePublicId,
      Arg.Is<IReadOnlyCollection<Guid>>(collection => collection.Count == 2 && collection.Contains(userA) && collection.Contains(userB)),
      true,
      cancellationToken);
  }

  [Fact]
  public async Task ExecuteAsync_WhenDryRunFalse_ShouldKeepDryRunFalseInResponse()
  {
    var rolePublicId = Guid.NewGuid();
    var userPublicId = Guid.NewGuid();

    _assignmentService
      .UpdateRoleUsersAsync(rolePublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(new RoleUserAssignmentBulkResult(
        OwnerPublicId: rolePublicId,
        DryRun: false,
        TotalRequested: 1,
        TotalAssignedNew: 0,
        TotalAssignedReactivated: 0,
        TotalUnassigned: 0,
        TotalUnchanged: 1,
        ExecutedAt: DateTime.UtcNow,
        RequestedPublicIds: [userPublicId],
        AssignedNewPublicIds: [],
        AssignedReactivatedPublicIds: [],
        UnassignedPublicIds: [],
        UnchangedPublicIds: [userPublicId])));

    var result = await _service.ExecuteAsync(rolePublicId, [userPublicId], dryRun: false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.DryRun.Should().BeFalse();
  }
}


