using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;

public static class GetPermissionsByPublicIdDI
{
  public static IServiceCollection AddGetPermissionsByPublicIdDI(this IServiceCollection services)
  {
    services.AddScoped<IGetPermissionsByPublicIdRepository, GetPermissionsByPublicIdRepository>();
    services.AddScoped<IGetPermissionsByPublicIdService, GetPermissionsByPublicIdService>();
    services.AddScoped<GetPermissionsByPublicIdHandler>();
    return services;
  }
}
