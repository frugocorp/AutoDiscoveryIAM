using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public static class UpdateBulkUserRolesDI
{
  public static IServiceCollection AddUpdateBulkUserRolesDI(this IServiceCollection services)
  {
    services.AddScoped<IUpdateBulkUserRolesService, UpdateBulkUserRolesService>();
    services.AddScoped<UpdateBulkUserRolesHandler>();
    services.AddScoped<UpdateBulkUserRolesCommandValidator>();
    return services;
  }
}
