// -----------------------------------------------------------------------
// <copyright file="RefreshTokenCookieIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Refresh token en cookie HttpOnly (<c>RefreshTokenSettings:Delivery = Cookie</c>): login, refresh y
/// logout sin que el token pase por el cuerpo JSON.
/// </summary>
[Collection("GlobalTestSetup")]
public class RefreshTokenCookieIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string CookieName = "adiam_refresh_token";
  private const string TestPassword = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private WebApplicationFactory<Program> CreateCookieFactory() => factory.WithWebHostBuilder(builder =>
    builder.ConfigureServices(services =>
      services.PostConfigure<RefreshTokenSettings>(settings => settings.Delivery = RefreshTokenDeliveryMode.Cookie)));

  /// <summary>
  /// El valor de la cookie del refresh token en la respuesta, o <c>null</c> si no se escribió.
  /// </summary>
  private static string? ReadRefreshCookie(HttpResponseMessage response)
  {
    if (!response.Headers.TryGetValues("Set-Cookie", out var values))
    {
      return null;
    }

    var cookie = values.FirstOrDefault(v => v.StartsWith(CookieName + "=", StringComparison.Ordinal));
    cookie?.Contains("httponly", StringComparison.OrdinalIgnoreCase).Should().BeTrue("la cookie del refresh token siempre es HttpOnly");
    return cookie?[(CookieName.Length + 1)..].Split(';')[0];
  }

  private static HttpRequestMessage RequestWithCookie(string route, string cookieValue, HttpContent? content = null, bool withRequiredHeader = true)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = content };
    request.Headers.Add("Cookie", $"{CookieName}={cookieValue}");
    if (withRequiredHeader)
    {
      request.Headers.Add(RefreshTokenDelivery.RequiredCookieRequestHeader, "XMLHttpRequest");
    }

    return request;
  }

  private static async Task<string> CreateUserAsync(WebApplicationFactory<Program> appFactory, CancellationToken cancellationToken)
  {
    var email = $"cookie-{Guid.NewGuid():N}@example.com";
    using var scope = appFactory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    dbContext.Users.Add(new User
    {
      Username = $"cookie-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = passwordHasher.HashPassword(TestPassword),
      IsActive = true,
      IsEmailVerified = true,
      Status = "active",
    });
    await dbContext.SaveChangesAsync(cancellationToken);
    return email;
  }

  [Fact(DisplayName = "Cookie: login y refresh entregan el token solo en cookie HttpOnly, y el refresh la lee sin cuerpo")]
  public async Task LoginAndRefresh_UseHttpOnlyCookieOnly()
  {
    using var cookieFactory = CreateCookieFactory();
    var email = await CreateUserAsync(cookieFactory, _cancellationToken);
    using var client = cookieFactory.CreateClient();

    var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, TestPassword), _cancellationToken);
    login.StatusCode.Should().Be(HttpStatusCode.OK);
    (await login.Content.ReadFromJsonAsync<LoginResponse>(_cancellationToken))!.Data!.RefreshToken.Should().BeNull();
    var loginCookie = ReadRefreshCookie(login);
    loginCookie.Should().NotBeNullOrEmpty();

    // Sin cuerpo: el token llega solo en la cookie.
    using var refreshRequest = RequestWithCookie("/auth/refresh", loginCookie!);
    var refresh = await client.SendAsync(refreshRequest, _cancellationToken);
    refresh.StatusCode.Should().Be(HttpStatusCode.OK);
    var refreshBody = (await refresh.Content.ReadFromJsonAsync<RefreshTokenResponse>(_cancellationToken))!.Data!;
    refreshBody.RefreshToken.Should().BeNull();
    refreshBody.AccessToken.Should().NotBeNullOrWhiteSpace();
    var rotatedCookie = ReadRefreshCookie(refresh);
    rotatedCookie.Should().NotBeNullOrEmpty();
    rotatedCookie.Should().NotBe(loginCookie);
  }

  [Fact(DisplayName = "Cookie: sin el encabezado X-Requested-With el refresh responde 400, no rota ni borra la cookie")]
  public async Task Refresh_FromCookieWithoutRequiredHeader_IsRejectedWithoutRotating()
  {
    using var cookieFactory = CreateCookieFactory();
    var email = await CreateUserAsync(cookieFactory, _cancellationToken);
    using var client = cookieFactory.CreateClient();
    var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, TestPassword), _cancellationToken);
    var cookie = ReadRefreshCookie(login)!;

    using var forged = RequestWithCookie("/auth/refresh", cookie, withRequiredHeader: false);
    var rejected = await client.SendAsync(forged, _cancellationToken);
    rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    ReadRefreshCookie(rejected).Should().BeNull("una petición ajena no debe borrar ni reemplazar la cookie");

    // El token sigue vigente: no se rotó ni se revocó la sesión.
    using var legit = RequestWithCookie("/auth/refresh", cookie);
    (await client.SendAsync(legit, _cancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact(DisplayName = "Cookie: con el token en el cuerpo no se exige el encabezado X-Requested-With")]
  public async Task Refresh_WithBodyToken_DoesNotRequireHeader()
  {
    using var bothFactory = factory.WithWebHostBuilder(builder =>
      builder.ConfigureServices(services =>
        services.PostConfigure<RefreshTokenSettings>(settings => settings.Delivery = RefreshTokenDeliveryMode.Both)));
    var email = await CreateUserAsync(bothFactory, _cancellationToken);
    using var client = bothFactory.CreateClient();
    var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, TestPassword), _cancellationToken);
    var bodyToken = (await login.Content.ReadFromJsonAsync<LoginResponse>(_cancellationToken))!.Data!.RefreshToken!;

    var refresh = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(bodyToken), _cancellationToken);

    refresh.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact(DisplayName = "Cookie: el logout revoca el token de la cookie y la borra")]
  public async Task Logout_RevokesCookieTokenAndClearsCookie()
  {
    using var cookieFactory = CreateCookieFactory();
    var email = await CreateUserAsync(cookieFactory, _cancellationToken);
    using var client = cookieFactory.CreateClient();

    var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, TestPassword), _cancellationToken);
    var accessToken = (await login.Content.ReadFromJsonAsync<LoginResponse>(_cancellationToken))!.Data!.Token;
    var cookie = ReadRefreshCookie(login)!;

    using var logoutRequest = RequestWithCookie("/auth/logout", cookie, JsonContent.Create(new LogoutRequestDto()));
    logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var logout = await client.SendAsync(logoutRequest, _cancellationToken);
    logout.StatusCode.Should().Be(HttpStatusCode.OK);
    ReadRefreshCookie(logout).Should().BeEmpty("el logout borra la cookie");

    using var refreshRequest = RequestWithCookie("/auth/refresh", cookie);
    var refresh = await client.SendAsync(refreshRequest, _cancellationToken);
    refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }
}
