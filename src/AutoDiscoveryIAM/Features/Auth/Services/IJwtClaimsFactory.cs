// -----------------------------------------------------------------------
// <copyright file="IJwtClaimsFactory.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;

/// <summary>
/// Servicio único para la generación de claims y access tokens JWT, evitando duplicar
/// esta lógica entre Login, ExternalLogin y RefreshToken.
/// </summary>
public interface IJwtClaimsFactory
{
  IReadOnlyList<Claim> BuildClaims(User user);

  (string Token, DateTime ExpiresAt) BuildAccessToken(User user);
}
