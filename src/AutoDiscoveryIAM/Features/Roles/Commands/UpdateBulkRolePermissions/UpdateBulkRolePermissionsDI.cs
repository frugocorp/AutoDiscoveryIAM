using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public static class UpdateBulkRolePermissionsDI
{
    public static IServiceCollection AddUpdateBulkRolePermissionsDI(this IServiceCollection services)
    {
        services.AddScoped<IUpdateBulkRolePermissionsRepository, UpdateBulkRolePermissionsRepository>();
        services.AddScoped<IUpdateBulkRolePermissionsService, UpdateBulkRolePermissionsService>();
        services.AddScoped<UpdateBulkRolePermissionsHandler>();
        services.AddScoped<UpdateBulkRolePermissionsCommandValidator>();
        return services;
    }
}
