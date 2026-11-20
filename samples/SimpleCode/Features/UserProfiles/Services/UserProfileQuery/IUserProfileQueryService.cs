using TuProyecto.Features.UserProfiles.Models.Entities;

namespace TuProyecto.Features.UserProfiles.Services.UserProfileQuery;

/// <summary>
/// Consultas genéricas del dominio UserProfiles, reutilizables por cualquier slice que las necesite
/// -- no exclusivas de CreateUserProfile ni de GetUserProfiles. Vive un nivel arriba de
/// Commands/Queries (a nivel de la feature completa), pero sigue siendo parte del dominio
/// UserProfiles, no un servicio genérico de toda la app -- esto no rompe Vertical Slice: la
/// separación de dominio sigue existiendo, solo que a nivel de feature en vez de a nivel de slice,
/// evitando reescribir la misma consulta (ej. "buscar un perfil por PublicId") en cada slice futuro
/// que la necesite (UpdateUserProfile, DeleteUserProfile, etc.). Mismo patrón que usa la librería en
/// Features/Roles/Services/RoleQuery/.
/// </summary>
public interface IUserProfileQueryService
{
  Task<UserProfile?> GetUserProfileByPublicIdAsync(Guid userProfilePublicId);
  Task<bool> UserProfileExistsAsync(Guid userProfilePublicId);
}
