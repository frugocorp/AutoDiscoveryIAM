using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;

public static class TwoFactorDI
{
  public static IServiceCollection AddTwoFactorDI(this IServiceCollection services)
  {
    services.AddScoped<ITwoFactorRepository, TwoFactorRepository>();
    services.AddScoped<ITwoFactorService, TwoFactorService>();

    services.AddScoped<EnableEmailTwoFactorRequestValidator>();
    services.AddScoped<IValidator<EnableEmailTwoFactorRequestDto>, EnableEmailTwoFactorRequestValidator>();
    services.AddScoped<EnrollAuthenticatorRequestValidator>();
    services.AddScoped<IValidator<EnrollAuthenticatorRequestDto>, EnrollAuthenticatorRequestValidator>();
    services.AddScoped<ConfirmAuthenticatorRequestValidator>();
    services.AddScoped<IValidator<ConfirmAuthenticatorRequestDto>, ConfirmAuthenticatorRequestValidator>();
    services.AddScoped<DisableTwoFactorRequestValidator>();
    services.AddScoped<IValidator<DisableTwoFactorRequestDto>, DisableTwoFactorRequestValidator>();

    return services;
  }
}
