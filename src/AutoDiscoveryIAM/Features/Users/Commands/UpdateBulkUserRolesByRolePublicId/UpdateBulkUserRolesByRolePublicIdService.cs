using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public interface IUpdateBulkUserRolesByRolePublicIdService
{
    Task<OperationResult<UpdateBulkUserRolesByRolePublicIdResponseDto>> ExecuteAsync(Guid rolePublicId, IReadOnlyCollection<Guid> userPublicIds, bool dryRun, CancellationToken cancellationToken);
}

public class UpdateBulkUserRolesByRolePublicIdService(IRoleUserAssignmentService roleUserAssignmentService) : IUpdateBulkUserRolesByRolePublicIdService
{
    private readonly IRoleUserAssignmentService _roleUserAssignmentService = roleUserAssignmentService;

    public async Task<OperationResult<UpdateBulkUserRolesByRolePublicIdResponseDto>> ExecuteAsync(Guid rolePublicId, IReadOnlyCollection<Guid> userPublicIds, bool dryRun, CancellationToken cancellationToken)
    {
        var result = await _roleUserAssignmentService.UpdateRoleUsersAsync(rolePublicId, userPublicIds, dryRun, cancellationToken);
        if (!result.Success)
        {
            return OperationResult.Fail<UpdateBulkUserRolesByRolePublicIdResponseDto>(result.ErrorInfo!, result.Error!);
        }

        var data = result.Data!;
        return OperationResult.Ok(new UpdateBulkUserRolesByRolePublicIdResponseDto(
          RolePublicId: rolePublicId,
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
