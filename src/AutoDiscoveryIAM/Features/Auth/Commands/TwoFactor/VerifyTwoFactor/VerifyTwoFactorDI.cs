// -----------------------------------------------------------------------
// <copyright file="VerifyTwoFactorDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

public static class VerifyTwoFactorDI
{
  public static IServiceCollection AddVerifyTwoFactorDI(this IServiceCollection services)
  {
    services.AddScoped<VerifyTwoFactorHandler>();
    services.AddScoped<VerifyTwoFactorCommandValidator>();
    services.AddScoped<IValidator<VerifyTwoFactorCommand>, VerifyTwoFactorCommandValidator>();

    return services;
  }
}
