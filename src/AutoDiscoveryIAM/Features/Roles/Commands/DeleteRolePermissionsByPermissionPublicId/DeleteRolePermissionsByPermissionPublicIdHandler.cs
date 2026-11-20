using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public class DeleteRolePermissionsByPermissionPublicIdHandler(IDeleteRolePermissionsByPermissionPublicIdService service)
{
    private readonly IDeleteRolePermissionsByPermissionPublicIdService _service = service;

    public Task<OperationResult<DeleteRolePermissionsByPermissionPublicIdResponseDto>> Handle(DeleteRolePermissionsByPermissionPublicIdCommand command, CancellationToken cancellationToken)
      => _service.ExecuteAsync(command.RolePublicId, command.PermissionPublicId, cancellationToken);
}
