using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public class CreateUserRolesHandler(ICreateUserRolesService service)
{
  private readonly ICreateUserRolesService _service = service;

  public Task<OperationResult<CreateUserRolesResponseDto>> Handle(CreateUserRolesCommand command, CancellationToken cancellationToken)
  {
    return _service.ExecuteAsync(command.UserPublicId, command.RolePublicId, cancellationToken);
  }
}
