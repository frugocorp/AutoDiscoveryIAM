using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public static class CreateRolePermissionsDI
{
    public static IServiceCollection AddCreateRolePermissionsDI(this IServiceCollection services)
    {
        services.AddScoped<ICreateRolePermissionsRepository, CreateRolePermissionsRepository>();
        services.AddScoped<ICreateRolePermissionsService, CreateRolePermissionsService>();
        services.AddScoped<CreateRolePermissionsHandler>();
        services.AddScoped<CreateRolePermissionsCommandValidator>();
        return services;
    }
}
