// -----------------------------------------------------------------------
// <copyright file="LoginEndPointMapping.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;
using Microsoft.AspNetCore.Routing;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth;

/// <summary>
/// Registro de todos los endpoints de Auth en el pipeline de Minimal API.
/// </summary>
public static class AuthEndPointMapping
{
  public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
  {
    LoginEndPoint.MapLoginEndpoint(app);
    GoogleAuthEndpoints.MapGoogleAuthEndpoints(app);
    ExchangeLoginCodeEndPoint.MapExchangeLoginCodeEndpoint(app);
    RefreshTokenEndpoint.MapRefreshTokenEndpoint(app);
    LogoutEndpoint.MapLogoutEndpoint(app);
    VerifyTwoFactorEndpoint.MapVerifyTwoFactorEndpoint(app);
  }
}
