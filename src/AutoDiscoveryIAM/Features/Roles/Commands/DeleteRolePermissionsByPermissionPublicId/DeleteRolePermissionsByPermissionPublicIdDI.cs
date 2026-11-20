using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public static class DeleteRolePermissionsByPermissionPublicIdDI
{
    public static IServiceCollection AddDeleteRolePermissionsByPermissionPublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IDeleteRolePermissionsByPermissionPublicIdRepository, DeleteRolePermissionsByPermissionPublicIdRepository>();
        services.AddScoped<IDeleteRolePermissionsByPermissionPublicIdService, DeleteRolePermissionsByPermissionPublicIdService>();
        services.AddScoped<DeleteRolePermissionsByPermissionPublicIdHandler>();
        services.AddScoped<DeleteRolePermissionsByPermissionPublicIdCommandValidator>();
        return services;
    }
}
