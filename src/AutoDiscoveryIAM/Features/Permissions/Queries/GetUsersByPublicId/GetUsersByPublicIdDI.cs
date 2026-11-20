namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;

public static class GetUsersByPublicIdDI
{
    public static IServiceCollection AddGetUsersByPublicId(this IServiceCollection services)
    {
        services.AddScoped<IGetUsersByPublicIdRepository, GetUsersByPublicIdRepository>();
        services.AddScoped<IGetUsersByPublicIdService, GetUsersByPublicIdService>();
        services.AddScoped<GetUsersByPublicIdHandler>();

        return services;
    }
}
