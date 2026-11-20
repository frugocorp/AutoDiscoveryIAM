namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;

public record RestoreUserRolesByPublicIdResponseDto(Guid UserPublicId, Guid RolePublicId, DateTime RestoredAt);
