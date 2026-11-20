namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public record CreateUserRolesResponseDto(Guid UserPublicId, Guid RolePublicId, DateTime AssignedAt);
