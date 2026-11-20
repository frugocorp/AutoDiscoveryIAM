namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public static class CreateUserProfileDI
{
  public static IServiceCollection AddCreateUserProfileDI(this IServiceCollection services)
  {
    services.AddScoped<ICreateUserProfileRepository, CreateUserProfileRepository>();
    services.AddScoped<CreateUserProfileHandler>();
    services.AddScoped<CreateUserProfileCommandValidator>();
    return services;
  }
}
