namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;

public record GetMyPermissionsResponseDto(Guid PublicId, string Key, string? Description, string? Group);
