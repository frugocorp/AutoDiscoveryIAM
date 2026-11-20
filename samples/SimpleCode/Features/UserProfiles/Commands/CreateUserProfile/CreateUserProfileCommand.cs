namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileCommand(Guid UserPublicId, string Occupation, string Biography, string MaritalStatus);
