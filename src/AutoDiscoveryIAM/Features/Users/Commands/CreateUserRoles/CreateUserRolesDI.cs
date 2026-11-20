using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public static class CreateUserRolesDI
{
  public static IServiceCollection AddCreateUserRolesDI(this IServiceCollection services)
  {
    services.AddScoped<ICreateUserRolesRepository, CreateUserRolesRepository>();
    services.AddScoped<ICreateUserRolesService, CreateUserRolesService>();
    services.AddScoped<CreateUserRolesHandler>();
    services.AddScoped<CreateUserRolesCommandValidator>();
    return services;
  }
}
