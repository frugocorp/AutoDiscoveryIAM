namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;

public record GetPermissionsByPublicIdResponseDto(Guid PublicId, string Key, string? Description, string? Group);
