namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

/// <summary>
/// Solo expone el PublicId (del perfil y del usuario dueño), nunca los Id internos de la base de datos.
/// </summary>
public record CreateUserProfileResponseDto(Guid PublicId, Guid UserPublicId, string Occupation, string Biography, string MaritalStatus);
