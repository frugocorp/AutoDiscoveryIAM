using TuProyecto.Features.UserProfiles;

namespace TuProyecto.Features;

/// <summary>
/// Punto único de registro de TODAS las features de tu app -- Program.cs solo llama
/// AddFeaturesDI() una vez, sin importar cuántas features tengas. A medida que agregues features
/// nuevas (además de UserProfiles), solo agregas su propia línea aquí. Mismo patrón que usa la
/// librería en Features/FeaturesDependencyInjection.cs (ahí se llama AddFeaturesDI() también, y
/// agrega AddUsersDI()/AddAuthDI()/AddPermissionsDI()/AddRolesDI()).
/// </summary>
public static class FeaturesDI
{
  public static IServiceCollection AddFeaturesDI(this IServiceCollection services)
  {
    services.AddUserProfilesDI();
    // A medida que agregues features nuevas: services.AddOtraFeatureDI();
    return services;
  }
}
