using TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;
using TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

namespace TuProyecto.Features.UserProfiles;

/// <summary>
/// Registra los endpoints de toda la feature UserProfiles en el pipeline de Minimal API en un solo
/// lugar. Mismo patrón que usa la librería en Features/Roles/RolesEndPointMapping.cs.
/// </summary>
public static class UserProfilesEndPointMapping
{
  public static void MapUserProfilesEndpoints(this IEndpointRouteBuilder app)
  {
    CreateUserProfileEndPoint.MapCreateUserProfileEndpoint(app);
    GetUserProfilesEndPoint.MapGetUserProfilesEndpoint(app);
  }
}
