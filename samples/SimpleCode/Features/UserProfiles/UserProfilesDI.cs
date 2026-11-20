using TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;
using TuProyecto.Features.UserProfiles.Infrastructure.Logging;
using TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;
using TuProyecto.Features.UserProfiles.Services.UserProfileQuery;

namespace TuProyecto.Features.UserProfiles;

/// <summary>
/// Registra las dependencias de toda la feature UserProfiles en un solo lugar -- a medida que
/// agregues más slices (UpdateUserProfile, DeleteUserProfile, etc.), solo agregas su propia línea
/// aquí, sin tocar Program.cs. Mismo patrón que usa la librería en Features/Roles/RolesDI.cs.
/// </summary>
public static class UserProfilesDI
{
  public static IServiceCollection AddUserProfilesDI(this IServiceCollection services)
  {
    services.AddTransient<IUserProfileLogger, UserProfileLogger>();
    services.AddScoped<IUserProfileQueryService, UserProfileQueryService>();

    services.AddCreateUserProfileDI();
    services.AddGetUserProfilesDI();

    return services;
  }
}
