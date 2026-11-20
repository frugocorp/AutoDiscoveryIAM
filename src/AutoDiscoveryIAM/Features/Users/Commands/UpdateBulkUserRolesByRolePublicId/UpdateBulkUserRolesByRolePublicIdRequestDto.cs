namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public record UpdateBulkUserRolesByRolePublicIdRequestDto(IReadOnlyCollection<Guid> UserPublicIds, bool DryRun = false);
