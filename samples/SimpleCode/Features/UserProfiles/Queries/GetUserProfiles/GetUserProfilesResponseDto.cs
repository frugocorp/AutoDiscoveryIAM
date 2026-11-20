namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

/// <summary>
/// Username/Email vienen de User (la librería) via el join nativo en GetUserProfilesRepository --
/// la prueba en código de por qué el contexto único evita joins en memoria/SQL directo para este
/// tipo de consulta.
/// </summary>
public record GetUserProfilesResponseDto(
  Guid PublicId,
  Guid UserPublicId,
  string Username,
  string Email,
  string Occupation,
  string Biography,
  string MaritalStatus,
  DateTime CreatedAt);
