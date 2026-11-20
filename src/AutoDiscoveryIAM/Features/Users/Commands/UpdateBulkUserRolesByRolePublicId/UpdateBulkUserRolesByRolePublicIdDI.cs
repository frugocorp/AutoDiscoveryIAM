using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public static class UpdateBulkUserRolesByRolePublicIdDI
{
    public static IServiceCollection AddUpdateBulkUserRolesByRolePublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IUpdateBulkUserRolesByRolePublicIdService, UpdateBulkUserRolesByRolePublicIdService>();
        services.AddScoped<UpdateBulkUserRolesByRolePublicIdHandler>();
        services.AddScoped<UpdateBulkUserRolesByRolePublicIdCommandValidator>();
        return services;
    }
}
