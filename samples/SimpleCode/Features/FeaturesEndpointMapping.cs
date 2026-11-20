using TuProyecto.Features.UserProfiles;

namespace TuProyecto.Features;

/// <summary>
/// Punto único de registro de los endpoints de TODAS las features de tu app -- Program.cs solo
/// llama MapAllFeatureEndpoints() una vez. Mismo patrón que usa la librería en
/// Features/FeaturesEndpointMapping.cs (MapAllFeatureEndpoints() ahí llama MapUsersEndpoints()/
/// MapAuthEndpoints()/MapPermissionsEndpoints()/MapRolesEndpoints()).
/// </summary>
public static class FeaturesEndpointMapping
{
  public static void MapAllFeatureEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapUserProfilesEndpoints();
    // A medida que agregues features nuevas: app.MapOtraFeatureEndpoints();
  }
}
