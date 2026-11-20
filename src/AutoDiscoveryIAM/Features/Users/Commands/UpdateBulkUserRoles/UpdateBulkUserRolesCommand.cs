namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public record UpdateBulkUserRolesCommand(Guid UserPublicId, IReadOnlyCollection<Guid> RolePublicIds, bool DryRun);
