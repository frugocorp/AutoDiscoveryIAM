using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public class CreateRolePermissionsHandler(ICreateRolePermissionsService service)
{
    private readonly ICreateRolePermissionsService _service = service;

    public Task<OperationResult<CreateRolePermissionsResponseDto>> Handle(CreateRolePermissionsCommand command, CancellationToken cancellationToken)
      => _service.ExecuteAsync(command.RolePublicId, command.PermissionPublicId, cancellationToken);
}
