namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public record CreateRolePermissionsResponseDto(Guid RolePublicId, Guid PermissionPublicId, DateTime AssignedAt);
