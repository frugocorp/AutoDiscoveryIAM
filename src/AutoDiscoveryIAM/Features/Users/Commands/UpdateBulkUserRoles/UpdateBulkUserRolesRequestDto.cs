namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public record UpdateBulkUserRolesRequestDto(IReadOnlyCollection<Guid> RolePublicIds, bool DryRun = false);
