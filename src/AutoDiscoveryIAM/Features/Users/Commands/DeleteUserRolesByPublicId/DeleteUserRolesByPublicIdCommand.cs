namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public record DeleteUserRolesByPublicIdCommand(Guid UserPublicId, Guid RolePublicId);
