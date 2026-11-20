// -----------------------------------------------------------------------
// <copyright file="JwtClaimsFactory.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;

public sealed class JwtClaimsFactory(IOptions<JwtSettings> jwtSettings) : IJwtClaimsFactory
{
  private readonly JwtSettings _jwtSettings = jwtSettings.Value;

  public IReadOnlyList<Claim> BuildClaims(User user)
  {
    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, user.PublicId.ToString()),
      new(ClaimTypes.Email, user.Email),
      new(ClaimTypes.Name, user.Username),
      new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      new(JwtClaimTypes.TokenValidFrom, ToUnixTimeSeconds(user.TokenValidFrom).ToString(System.Globalization.CultureInfo.InvariantCulture)),
    };

    var activeRoles = user.UserRoles
      .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
      .Select(ur => ur.Role!.Name);

    foreach (var roleName in activeRoles)
    {
      claims.Add(new Claim(ClaimTypes.Role, roleName));
    }

    return claims;
  }

  public (string Token, DateTime ExpiresAt) BuildAccessToken(User user)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = _jwtSettings.GetSigningKeyBytes();
    var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(BuildClaims(user)),
      Expires = expiresAt,
      SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key),
        SecurityAlgorithms.HmacSha256Signature),
      Issuer = _jwtSettings.Issuer,
      Audience = _jwtSettings.Audience,
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return (tokenString, expiresAt);
  }

  /// <summary>
  /// Convierte una fecha en horario de México (GMT-6) a segundos Unix (UTC), para que el claim
  /// del token sea comparable sin ambigüedad de zona horaria al validarlo.
  /// </summary>
  private static long ToUnixTimeSeconds(DateTime mexicoDateTime)
  {
    var utcDateTime = TimeHelpers.ConvertMexicoTimeToUtc(mexicoDateTime);
    return new DateTimeOffset(utcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();
  }
}
