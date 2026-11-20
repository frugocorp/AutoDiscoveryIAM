namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public record UpdateBulkUserRolesResponseDto(
    Guid UserPublicId,
    bool DryRun,
    int TotalRequested,
    int TotalAssignedNew,
    int TotalAssignedReactivated,
    int TotalUnassigned,
    int TotalUnchanged,
    DateTime ExecutedAt,
    IReadOnlyList<Guid> RequestedPublicIds,
    IReadOnlyList<Guid> AssignedNewPublicIds,
    IReadOnlyList<Guid> AssignedReactivatedPublicIds,
    IReadOnlyList<Guid> UnassignedPublicIds,
    IReadOnlyList<Guid> UnchangedPublicIds);
