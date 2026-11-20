using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;

public static class GetPermissionsByPublicIdDI
{
    public static IServiceCollection AddGetRolePermissionsByPublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IGetPermissionsByPublicIdRepository, GetPermissionsByPublicIdRepository>();
        services.AddScoped<IGetPermissionsByPublicIdService, GetPermissionsByPublicIdService>();
        services.AddScoped<GetPermissionsByPublicIdHandler>();
        return services;
    }
}
