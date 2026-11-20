namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;

public record GetRolesByPublicIdResponseDto(Guid PublicId, string Name, string? Description, DateTime AssignedAt);
