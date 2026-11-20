namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;

public record GetUsersByPublicIdResponseDto(Guid PublicId, string Username, string Email, DateTime AssignedAt);
