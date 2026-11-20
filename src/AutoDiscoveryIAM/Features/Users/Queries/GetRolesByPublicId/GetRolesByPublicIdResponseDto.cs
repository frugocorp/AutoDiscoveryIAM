namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;

public record GetRolesByPublicIdResponseDto(Guid PublicId, string Name, string? Description, bool IsActive, DateTime AssignedAt);
