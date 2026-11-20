using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public class UpdateBulkUserRolesByRolePublicIdHandler(IUpdateBulkUserRolesByRolePublicIdService service)
{
    private readonly IUpdateBulkUserRolesByRolePublicIdService _service = service;

    public Task<OperationResult<UpdateBulkUserRolesByRolePublicIdResponseDto>> Handle(UpdateBulkUserRolesByRolePublicIdCommand command, CancellationToken cancellationToken)
      => _service.ExecuteAsync(command.RolePublicId, command.UserPublicIds, command.DryRun, cancellationToken);
}
