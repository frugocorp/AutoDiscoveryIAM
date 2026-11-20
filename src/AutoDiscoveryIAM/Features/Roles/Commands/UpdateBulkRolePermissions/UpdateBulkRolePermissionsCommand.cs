namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public record UpdateBulkRolePermissionsCommand(Guid RolePublicId, IReadOnlyCollection<Guid> PermissionPublicIds, bool DryRun);
