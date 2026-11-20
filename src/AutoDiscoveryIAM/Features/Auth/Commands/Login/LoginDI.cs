// -----------------------------------------------------------------------
// <copyright file="LoginDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

/// <summary>
/// Registro de dependencias para el comando Login.
/// </summary>
public static class LoginDI
{
  public static IServiceCollection AddLoginDI(this IServiceCollection services)
  {
    services.AddScoped<LoginHandler>();
    services.AddScoped<LoginCommandValidator>();
    services.AddScoped<IAuthLogger, AuthLogger>();
    return services;
  }
}
