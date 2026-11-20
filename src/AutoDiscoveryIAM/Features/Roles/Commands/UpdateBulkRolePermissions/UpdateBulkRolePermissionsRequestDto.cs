namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public record UpdateBulkRolePermissionsRequestDto(IReadOnlyCollection<Guid> PermissionPublicIds, bool DryRun = false);
