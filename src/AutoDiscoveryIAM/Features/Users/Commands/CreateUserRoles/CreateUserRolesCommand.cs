namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public record CreateUserRolesCommand(Guid UserPublicId, Guid RolePublicId);
