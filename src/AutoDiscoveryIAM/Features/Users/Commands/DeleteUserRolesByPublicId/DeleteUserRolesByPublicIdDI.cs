using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public static class DeleteUserRolesByPublicIdDI
{
    public static IServiceCollection AddDeleteUserRolesByPublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IDeleteUserRolesByPublicIdRepository, DeleteUserRolesByPublicIdRepository>();
        services.AddScoped<IDeleteUserRolesByPublicIdService, DeleteUserRolesByPublicIdService>();
        services.AddScoped<DeleteUserRolesByPublicIdHandler>();
        services.AddScoped<DeleteUserRolesByPublicIdCommandValidator>();
        return services;
    }
}
