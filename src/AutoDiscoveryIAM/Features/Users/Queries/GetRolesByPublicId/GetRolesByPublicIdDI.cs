using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;

public static class GetRolesByPublicIdDI
{
  public static IServiceCollection AddGetRolesByPublicIdDI(this IServiceCollection services)
  {
    services.AddScoped<IGetRolesByPublicIdRepository, GetRolesByPublicIdRepository>();
    services.AddScoped<IGetRolesByPublicIdService, GetRolesByPublicIdService>();
    services.AddScoped<GetRolesByPublicIdHandler>();
    return services;
  }
}
