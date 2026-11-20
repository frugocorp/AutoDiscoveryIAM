using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public class UpdateBulkUserRolesHandler(IUpdateBulkUserRolesService service)
{
  private readonly IUpdateBulkUserRolesService _service = service;

  public Task<OperationResult<UpdateBulkUserRolesResponseDto>> Handle(UpdateBulkUserRolesCommand command, CancellationToken cancellationToken)
  {
    return _service.ExecuteAsync(command.UserPublicId, command.RolePublicIds, command.DryRun, cancellationToken);
  }
}
