namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public record CreateRolePermissionsCommand(Guid RolePublicId, Guid PermissionPublicId);
