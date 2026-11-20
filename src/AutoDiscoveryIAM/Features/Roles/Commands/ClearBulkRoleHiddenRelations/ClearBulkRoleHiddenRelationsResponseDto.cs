namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public record ClearBulkRoleHiddenRelationsResponseDto(
    Guid RolePublicId,
    int ClearedUserRelations,
    int ClearedPermissionRelations,
    int TotalCleared,
    DateTime ExecutedAt);
