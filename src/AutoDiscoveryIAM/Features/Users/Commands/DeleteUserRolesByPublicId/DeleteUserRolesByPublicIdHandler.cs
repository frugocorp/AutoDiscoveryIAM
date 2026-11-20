using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public class DeleteUserRolesByPublicIdHandler(IDeleteUserRolesByPublicIdService service)
{
    private readonly IDeleteUserRolesByPublicIdService _service = service;

    public Task<OperationResult<DeleteUserRolesByPublicIdResponseDto>> Handle(DeleteUserRolesByPublicIdCommand command, CancellationToken cancellationToken)
    {
        return _service.ExecuteAsync(command.UserPublicId, command.RolePublicId, cancellationToken);
    }
}
