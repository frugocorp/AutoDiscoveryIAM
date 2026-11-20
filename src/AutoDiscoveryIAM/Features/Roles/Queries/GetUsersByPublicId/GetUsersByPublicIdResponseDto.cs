namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;

public record GetUsersByPublicIdResponseDto(Guid PublicId, string Username, string Email, DateTime AssignedAt);
