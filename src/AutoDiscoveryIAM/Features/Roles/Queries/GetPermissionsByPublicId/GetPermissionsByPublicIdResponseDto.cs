namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;

public record GetPermissionsByPublicIdResponseDto(Guid PublicId, string Key, string? Description, string? Group, DateTime AssignedAt);
