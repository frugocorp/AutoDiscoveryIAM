namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public static class GetUserProfilesDI
{
  public static IServiceCollection AddGetUserProfilesDI(this IServiceCollection services)
  {
    services.AddScoped<IGetUserProfilesRepository, GetUserProfilesRepository>();
    services.AddScoped<GetUserProfilesHandler>();
    return services;
  }
}
