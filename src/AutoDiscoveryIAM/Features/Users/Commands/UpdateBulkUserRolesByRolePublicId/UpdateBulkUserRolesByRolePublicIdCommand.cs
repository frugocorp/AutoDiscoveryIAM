namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public record UpdateBulkUserRolesByRolePublicIdCommand(Guid RolePublicId, IReadOnlyCollection<Guid> UserPublicIds, bool DryRun);
