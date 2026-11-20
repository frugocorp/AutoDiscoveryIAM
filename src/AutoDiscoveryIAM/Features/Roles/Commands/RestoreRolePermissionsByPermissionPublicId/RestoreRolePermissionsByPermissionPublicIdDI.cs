using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;

public static class RestoreRolePermissionsByPermissionPublicIdDI
{
    public static IServiceCollection AddRestoreRolePermissionsByPermissionPublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IRestoreRolePermissionsByPermissionPublicIdRepository, RestoreRolePermissionsByPermissionPublicIdRepository>();
        services.AddScoped<IRestoreRolePermissionsByPermissionPublicIdService, RestoreRolePermissionsByPermissionPublicIdService>();
        return services;
    }
}
