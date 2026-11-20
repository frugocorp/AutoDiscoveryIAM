namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;

public static class GetRolesByPublicIdDI
{
    public static IServiceCollection AddGetRolesByPublicId(this IServiceCollection services)
    {
        services.AddScoped<IGetRolesByPublicIdRepository, GetRolesByPublicIdRepository>();
        services.AddScoped<IGetRolesByPublicIdService, GetRolesByPublicIdService>();
        services.AddScoped<GetRolesByPublicIdHandler>();

        return services;
    }
}
