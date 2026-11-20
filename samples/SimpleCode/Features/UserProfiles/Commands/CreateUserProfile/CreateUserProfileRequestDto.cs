namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileRequestDto(Guid UserPublicId, string Occupation, string Biography, string MaritalStatus);
