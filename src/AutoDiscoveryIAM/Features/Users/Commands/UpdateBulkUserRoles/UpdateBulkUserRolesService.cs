using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public interface IUpdateBulkUserRolesService
{
  Task<OperationResult<UpdateBulkUserRolesResponseDto>> ExecuteAsync(Guid userPublicId, IReadOnlyCollection<Guid> rolePublicIds, bool dryRun, CancellationToken cancellationToken);
}

public class UpdateBulkUserRolesService(IRoleUserAssignmentService roleUserAssignmentService) : IUpdateBulkUserRolesService
{
  private readonly IRoleUserAssignmentService _roleUserAssignmentService = roleUserAssignmentService;

  public async Task<OperationResult<UpdateBulkUserRolesResponseDto>> ExecuteAsync(Guid userPublicId, IReadOnlyCollection<Guid> rolePublicIds, bool dryRun, CancellationToken cancellationToken)
  {
    var result = await _roleUserAssignmentService.UpdateUserRolesAsync(userPublicId, rolePublicIds, dryRun, cancellationToken);
    if (!result.Success)
    {
      return OperationResult.Fail<UpdateBulkUserRolesResponseDto>(result.ErrorInfo!, result.Error!);
    }

    var data = result.Data!;
    return OperationResult.Ok(new UpdateBulkUserRolesResponseDto(
      UserPublicId: userPublicId,
      DryRun: data.DryRun,
      TotalRequested: data.TotalRequested,
      TotalAssignedNew: data.TotalAssignedNew,
      TotalAssignedReactivated: data.TotalAssignedReactivated,
      TotalUnassigned: data.TotalUnassigned,
      TotalUnchanged: data.TotalUnchanged,
      ExecutedAt: data.ExecutedAt,
      RequestedPublicIds: data.RequestedPublicIds,
      AssignedNewPublicIds: data.AssignedNewPublicIds,
      AssignedReactivatedPublicIds: data.AssignedReactivatedPublicIds,
      UnassignedPublicIds: data.UnassignedPublicIds,
      UnchangedPublicIds: data.UnchangedPublicIds));
  }
}
