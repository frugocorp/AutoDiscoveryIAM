using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateBulkUserRoles;

public class UpdateBulkUserRolesServiceTests
{
  private readonly IRoleUserAssignmentService _assignmentService;
  private readonly UpdateBulkUserRolesService _service;

  public UpdateBulkUserRolesServiceTests()
  {
    _assignmentService = Substitute.For<IRoleUserAssignmentService>();
    _service = new UpdateBulkUserRolesService(_assignmentService);
  }

  [Fact]
  public async Task ExecuteAsync_WhenAssignmentServiceFails_ShouldReturnSameError()
  {
    var userPublicId = Guid.NewGuid();
    var expectedError = string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.ActiveUserNotFound.Message, userPublicId);
    _assignmentService
      .UpdateUserRolesAsync(userPublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<RoleUserAssignmentBulkResult>(ErrorInfo.Status404NotFound, expectedError));

    var result = await _service.ExecuteAsync(userPublicId, [Guid.NewGuid()], dryRun: true, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(expectedError);
  }

  [Fact]
  public async Task ExecuteAsync_WhenAssignmentServiceSucceeds_ShouldMapAllScalarFields()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();
    var executedAt = DateTime.UtcNow;

    var bulkResult = new RoleUserAssignmentBulkResult(
      OwnerPublicId: userPublicId,
      DryRun: false,
      TotalRequested: 5,
      TotalAssignedNew: 2,
      TotalAssignedReactivated: 1,
      TotalUnassigned: 1,
      TotalUnchanged: 1,
      ExecutedAt: executedAt,
      RequestedPublicIds: [rolePublicId],
      AssignedNewPublicIds: [rolePublicId],
      AssignedReactivatedPublicIds: [],
      UnassignedPublicIds: [],
      UnchangedPublicIds: []);

    _assignmentService
      .UpdateUserRolesAsync(userPublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(bulkResult));

    var result = await _service.ExecuteAsync(userPublicId, [rolePublicId], dryRun: false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.UserPublicId.Should().Be(userPublicId);
    result.Data.DryRun.Should().BeFalse();
    result.Data.TotalRequested.Should().Be(5);
    result.Data.TotalAssignedNew.Should().Be(2);
    result.Data.TotalAssignedReactivated.Should().Be(1);
    result.Data.TotalUnassigned.Should().Be(1);
    result.Data.TotalUnchanged.Should().Be(1);
    result.Data.ExecutedAt.Should().Be(executedAt);
    result.Data.AssignedNewPublicIds.Should().ContainSingle().Which.Should().Be(rolePublicId);
  }

  [Fact]
  public async Task ExecuteAsync_WhenAssignmentServiceSucceeds_ShouldMapAllCollections()
  {
    var userPublicId = Guid.NewGuid();
    var requestedA = Guid.NewGuid();
    var requestedB = Guid.NewGuid();
    var newAssigned = Guid.NewGuid();
    var reactivated = Guid.NewGuid();
    var unassigned = Guid.NewGuid();
    var unchanged = Guid.NewGuid();

    var bulkResult = new RoleUserAssignmentBulkResult(
      OwnerPublicId: userPublicId,
      DryRun: true,
      TotalRequested: 2,
      TotalAssignedNew: 1,
      TotalAssignedReactivated: 1,
      TotalUnassigned: 1,
      TotalUnchanged: 1,
      ExecutedAt: DateTime.UtcNow,
      RequestedPublicIds: [requestedA, requestedB],
      AssignedNewPublicIds: [newAssigned],
      AssignedReactivatedPublicIds: [reactivated],
      UnassignedPublicIds: [unassigned],
      UnchangedPublicIds: [unchanged]);

    _assignmentService
      .UpdateUserRolesAsync(userPublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(bulkResult));

    var result = await _service.ExecuteAsync(userPublicId, [requestedA, requestedB], dryRun: true, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.RequestedPublicIds.Should().BeEquivalentTo(new[] { requestedA, requestedB });
    result.Data.AssignedNewPublicIds.Should().BeEquivalentTo(new[] { newAssigned });
    result.Data.AssignedReactivatedPublicIds.Should().BeEquivalentTo(new[] { reactivated });
    result.Data.UnassignedPublicIds.Should().BeEquivalentTo(new[] { unassigned });
    result.Data.UnchangedPublicIds.Should().BeEquivalentTo(new[] { unchanged });
  }

  [Fact]
  public async Task ExecuteAsync_ShouldForwardArgumentsToAssignmentService()
  {
    var userPublicId = Guid.NewGuid();
    var roleA = Guid.NewGuid();
    var roleB = Guid.NewGuid();
    var cancellationTokenSource = new CancellationTokenSource();
    var cancellationToken = cancellationTokenSource.Token;
    var requested = new[] { roleA, roleB };

    _assignmentService
      .UpdateUserRolesAsync(userPublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(new RoleUserAssignmentBulkResult(
        OwnerPublicId: userPublicId,
        DryRun: false,
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

    var result = await _service.ExecuteAsync(userPublicId, requested, dryRun: false, cancellationToken);

    result.Success.Should().BeTrue();
    await _assignmentService.Received(1).UpdateUserRolesAsync(
      userPublicId,
      Arg.Is<IReadOnlyCollection<Guid>>(collection => collection.Count == 2 && collection.Contains(roleA) && collection.Contains(roleB)),
      false,
      cancellationToken);
  }

  [Fact]
  public async Task ExecuteAsync_WhenDryRunTrue_ShouldKeepDryRunInResponse()
  {
    var userPublicId = Guid.NewGuid();
    var rolePublicId = Guid.NewGuid();

    _assignmentService
      .UpdateUserRolesAsync(userPublicId, Arg.Any<IReadOnlyCollection<Guid>>(), dryRun: true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(new RoleUserAssignmentBulkResult(
        OwnerPublicId: userPublicId,
        DryRun: true,
        TotalRequested: 1,
        TotalAssignedNew: 0,
        TotalAssignedReactivated: 0,
        TotalUnassigned: 0,
        TotalUnchanged: 1,
        ExecutedAt: DateTime.UtcNow,
        RequestedPublicIds: [rolePublicId],
        AssignedNewPublicIds: [],
        AssignedReactivatedPublicIds: [],
        UnassignedPublicIds: [],
        UnchangedPublicIds: [rolePublicId])));

    var result = await _service.ExecuteAsync(userPublicId, [rolePublicId], dryRun: true, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.DryRun.Should().BeTrue();
  }
}


