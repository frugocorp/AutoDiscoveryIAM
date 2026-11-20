// -----------------------------------------------------------------------
// <copyright file="ExternalLoginDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;

public static class ExternalLoginDI
{
    public static IServiceCollection AddExternalLoginFeature(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<ExternalLoginProviderResolver>();
        services.AddScoped<ExternalLoginSessionService>();
        services.AddScoped<ExternalLoginOrchestrator>();
        services.AddScoped<IExternalLoginProvider, GoogleProvider>();
        services.AddScoped<IExternalLoginExchangeCodeRepository, ExternalLoginExchangeCodeRepository>();
        services.AddExchangeLoginCodeDI();
        return services;
    }
}
