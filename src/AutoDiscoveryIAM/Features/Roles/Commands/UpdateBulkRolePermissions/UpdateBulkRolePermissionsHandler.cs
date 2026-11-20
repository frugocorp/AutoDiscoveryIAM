using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public class UpdateBulkRolePermissionsHandler(IUpdateBulkRolePermissionsService service)
{
    private readonly IUpdateBulkRolePermissionsService _service = service;

    public Task<OperationResult<UpdateBulkRolePermissionsResponseDto>> Handle(UpdateBulkRolePermissionsCommand command, CancellationToken cancellationToken)
      => _service.ExecuteAsync(command.RolePublicId, command.PermissionPublicIds, command.DryRun, cancellationToken);
}
