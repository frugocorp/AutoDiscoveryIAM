namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public record DeleteUserRolesByPublicIdResponseDto(Guid UserPublicId, Guid RolePublicId, DateTime UnassignedAt);
