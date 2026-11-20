using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;

public static class ActivationDI
{
    public static IServiceCollection AddActivationDI(this IServiceCollection services)
    {
        services.AddScoped<IActivationRepository, ActivationRepository>();
        services.AddScoped<IActivationTokenService, ActivationTokenService>();
        services.AddScoped<IActivationEmailService, ActivationEmailService>();
        services.AddScoped<IActivationService, ActivationService>();
        return services;
    }
}
