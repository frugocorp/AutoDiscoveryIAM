using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;

public static class GetMyPermissionsDI
{
  public static IServiceCollection AddGetMyPermissionsDI(this IServiceCollection services)
  {
    services.AddScoped<IGetMyPermissionsRepository, GetMyPermissionsRepository>();
    services.AddScoped<IGetMyPermissionsService, GetMyPermissionsService>();
    services.AddScoped<GetMyPermissionsHandler>();
    return services;
  }
}
