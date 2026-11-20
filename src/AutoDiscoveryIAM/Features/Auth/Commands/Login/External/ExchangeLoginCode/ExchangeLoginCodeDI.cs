// -----------------------------------------------------------------------
// <copyright file="ExchangeLoginCodeDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;

public static class ExchangeLoginCodeDI
{
  public static IServiceCollection AddExchangeLoginCodeDI(this IServiceCollection services)
  {
    services.AddScoped<ExchangeLoginCodeHandler>();
    services.AddScoped<ExchangeLoginCodeCommandValidator>();
    return services;
  }
}
