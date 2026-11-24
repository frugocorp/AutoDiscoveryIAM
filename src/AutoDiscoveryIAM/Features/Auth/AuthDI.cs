
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.TwoFactorStepUp;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth;

public static class AuthDI
{
  public static IServiceCollection AddAuthDI(this IServiceCollection services)
  {
    services.AddScoped<IJwtClaimsFactory, JwtClaimsFactory>();

    // Primitivas compartidas de 2FA (usadas por Login, VerifyTwoFactor, y la gestion de 2FA en Users)
    services.AddScoped<ITotpService, TotpService>();
    services.AddScoped<ITwoFactorCodeGenerator, TwoFactorCodeGenerator>();
    services.AddScoped<ITwoFactorEmailService, TwoFactorEmailService>();

    services.AddLoginDI();
    services.AddExternalLoginFeature();
    services.AddRefreshTokenFeature();
    services.AddLogoutFeature();
    services.AddVerifyTwoFactorDI();

    // Servicios de contexto de autenticación
    services.AddScoped<IAuthContextService, AuthContextService>();

    // Verificación step-up con el Authenticator, para las acciones sensibles de la app consumidora
    services.AddScoped<ITwoFactorStepUpService, TwoFactorStepUpService>();

    return services;
  }
}
