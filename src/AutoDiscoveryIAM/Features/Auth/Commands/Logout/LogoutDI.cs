// -----------------------------------------------------------------------
// <copyright file="LogoutDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// Configuración de Dependency Injection para Logout.
/// </summary>
public static class LogoutDI
{
  public static IServiceCollection AddLogoutFeature(this IServiceCollection services)
  {
    // Handler y Validador
    services.AddScoped<LogoutHandler>();
    services.AddScoped<LogoutCommandValidator>();
    services.AddScoped<IValidator<LogoutCommand>, LogoutCommandValidator>();

    return services;
  }
}
