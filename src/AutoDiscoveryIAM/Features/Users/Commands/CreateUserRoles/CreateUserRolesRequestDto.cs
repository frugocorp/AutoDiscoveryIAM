namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public record CreateUserRolesRequestDto(Guid UserPublicId, Guid RolePublicId);
