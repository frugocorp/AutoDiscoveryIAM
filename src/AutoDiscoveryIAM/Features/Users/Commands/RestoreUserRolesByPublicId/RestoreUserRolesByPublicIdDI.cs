using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;

public static class RestoreUserRolesByPublicIdDI
{
    public static IServiceCollection AddRestoreUserRolesByPublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IRestoreUserRolesByPublicIdRepository, RestoreUserRolesByPublicIdRepository>();
        services.AddScoped<IRestoreUserRolesByPublicIdService, RestoreUserRolesByPublicIdService>();
        return services;
    }
}
