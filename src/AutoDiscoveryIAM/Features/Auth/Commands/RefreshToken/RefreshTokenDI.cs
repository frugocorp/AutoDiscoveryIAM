// -----------------------------------------------------------------------
// <copyright file="RefreshTokenDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Configuración de Dependency Injection para RefreshToken.
/// </summary>
public static class RefreshTokenDI
{
  public static IServiceCollection AddRefreshTokenFeature(this IServiceCollection services)
  {
    // Configuración
    services.Configure<RefreshTokenSettings>(config =>
    {
      config.ExpiryDays = 7; // 7 días por defecto
      config.TokenLength = 32; // 32 bytes por defecto
    });

    // Servicios
    services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    services.AddScoped<IRefreshTokenGraceStore, RefreshTokenGraceStore>();
    services.AddScoped<IRefreshTokenDelivery, RefreshTokenDelivery>();

    // Handler y Validador
    services.AddScoped<RefreshTokenHandler>();
    services.AddScoped<RefreshTokenCommandValidator>();
    services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();

    return services;
  }
}
