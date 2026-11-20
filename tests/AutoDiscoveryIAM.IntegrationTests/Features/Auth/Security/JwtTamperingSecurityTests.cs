// -----------------------------------------------------------------------
// <copyright file="JwtTamperingSecurityTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Security;

/// <summary>
/// Prueba que el pipeline de autenticación JWT rechaza tokens manipulados de punta a punta contra
/// un endpoint real (<c>/users/me/permissions</c>, autenticado pero sin permiso RBAC específico
/// vía <c>[PermissionsNotRequired]</c>, para aislar la validación de autenticación de la de RBAC).
/// No existía ningún test end-to-end de esto: <c>JwtBlacklistServiceTests</c> solo prueba el
/// servicio de blacklist aislado, no que el middleware real rechace un token alterado.
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class JwtTamperingSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string ProtectedEndpoint = "/users/me/permissions";

  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact(DisplayName = "JWT: firma alterada es rechazada")]
  public async Task TamperedSignature_ShouldBeRejected()
  {
    var validToken = await GetValidTokenAsync();
    var segments = validToken.Split('.');
    segments[2] = FlipLastCharacter(segments[2]);
    var tamperedToken = string.Join('.', segments);

    var response = await SendWithTokenAsync(tamperedToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact(DisplayName = "JWT: token con alg=none es rechazado")]
  public async Task AlgNoneToken_ShouldBeRejected()
  {
    var headerJson = JsonSerializer.Serialize(new { alg = "none", typ = "JWT" });
    var payloadJson = JsonSerializer.Serialize(new
    {
      nameid = Guid.NewGuid().ToString(),
      email = "attacker@example.com",
      role = "AdminAutoDiscoveryIAM",
      exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
    });

    var unsignedToken = $"{Base64UrlEncoder.Encode(headerJson)}.{Base64UrlEncoder.Encode(payloadJson)}.";

    var response = await SendWithTokenAsync(unsignedToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact(DisplayName = "JWT: claims modificados sin re-firmar son rechazados")]
  public async Task TamperedPayloadClaims_ShouldBeRejected()
  {
    var validToken = await GetValidTokenAsync();
    var segments = validToken.Split('.');

    var payloadJson = Base64UrlEncoder.Decode(segments[1]);
    using var payloadDocument = JsonDocument.Parse(payloadJson);
    var mutablePayload = new Dictionary<string, object>();
    foreach (var property in payloadDocument.RootElement.EnumerateObject())
    {
      mutablePayload[property.Name] = property.Value.Clone();
    }

    // Intenta escalar privilegios inyectando un rol de administrador directamente en el payload,
    // sin volver a firmar el token con la clave real (que un atacante no tiene).
    mutablePayload[ClaimTypes.Role] = "AdminAutoDiscoveryIAM";

    segments[1] = Base64UrlEncoder.Encode(JsonSerializer.Serialize(mutablePayload));
    var tamperedToken = string.Join('.', segments);

    var response = await SendWithTokenAsync(tamperedToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact(DisplayName = "JWT: issuer distinto al configurado es rechazado, aun firmado con la clave real")]
  public async Task WrongIssuer_ShouldBeRejected()
  {
    var jwtSettings = GetJwtSettingsFromConfiguration();
    var token = BuildSignedToken(jwtSettings.Key, issuer: "issuer-falso", audience: jwtSettings.Audience, expiresAt: DateTime.UtcNow.AddMinutes(30));

    var response = await SendWithTokenAsync(token);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact(DisplayName = "JWT: audience distinta a la configurada es rechazada, aun firmado con la clave real")]
  public async Task WrongAudience_ShouldBeRejected()
  {
    var jwtSettings = GetJwtSettingsFromConfiguration();
    var token = BuildSignedToken(jwtSettings.Key, issuer: jwtSettings.Issuer, audience: "audience-falsa", expiresAt: DateTime.UtcNow.AddMinutes(30));

    var response = await SendWithTokenAsync(token);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact(DisplayName = "JWT: token expirado es rechazado, aun firmado con la clave real")]
  public async Task ExpiredToken_ShouldBeRejected()
  {
    var jwtSettings = GetJwtSettingsFromConfiguration();
    var token = BuildSignedToken(jwtSettings.Key, issuer: jwtSettings.Issuer, audience: jwtSettings.Audience, expiresAt: DateTime.UtcNow.AddMinutes(-5));

    var response = await SendWithTokenAsync(token);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  private async Task<string> GetValidTokenAsync()
  {
    var authenticated = await _client.AuthenticateUserAsync(UserType.Regular, _cancellationToken);
    return authenticated.Token!;
  }

  private async Task<HttpResponseMessage> SendWithTokenAsync(string token)
  {
    using var request = new HttpRequestMessage(HttpMethod.Get, ProtectedEndpoint);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return await _client.SendAsync(request, _cancellationToken);
  }

  private (string Key, string Issuer, string Audience) GetJwtSettingsFromConfiguration()
  {
    using var scope = _factory.Services.CreateScope();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var section = configuration.GetSection("JwtSettings");

    return (
      section["Key"] ?? throw new InvalidOperationException("JwtSettings:Key no configurado en el entorno de test."),
      section["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer no configurado en el entorno de test."),
      section["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience no configurado en el entorno de test."));
  }

  private static string BuildSignedToken(string key, string issuer, string audience, DateTime expiresAt)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var signingKey = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(key));

    var tokenDescriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]),
      // NotBefore explícito y anterior a Expires: permite construir un token ya expirado
      // (Expires en el pasado) sin que JwtSecurityTokenHandler lo rechace por NotBefore > Expires.
      NotBefore = expiresAt.AddMinutes(-10),
      Expires = expiresAt,
      SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature),
      Issuer = issuer,
      Audience = audience,
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
  }

  private static string FlipLastCharacter(string segment)
  {
    var chars = segment.ToCharArray();
    var lastIndex = chars.Length - 1;
    chars[lastIndex] = chars[lastIndex] == 'A' ? 'B' : 'A';
    return new string(chars);
  }
}
