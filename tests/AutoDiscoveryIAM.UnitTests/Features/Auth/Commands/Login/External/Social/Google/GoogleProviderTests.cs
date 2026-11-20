using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.External.Social.Google;

public class GoogleProviderTests
{
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private static GoogleAuthSettings ValidSettings() => new()
    {
        Enabled = true,
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RedirectUri = "https://app.example.com/callback",
    };

    private static GoogleProvider CreateSut(GoogleAuthSettings settings, Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(new HttpClient(new StubHttpMessageHandler(responder)));
        return new GoogleProvider(Options.Create(settings), httpClientFactory);
    }

    private static HttpResponseMessage TokenResponse(string idToken)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { id_token = idToken }) };

    private static HttpResponseMessage TokenInfoResponse(
      string sub = "google-sub-123",
      string email = "user@example.com",
      string emailVerified = "true",
      string aud = "client-id",
      string iss = "https://accounts.google.com",
      long? expUnixSeconds = null,
      string name = "Test User",
      string givenName = "Test",
      string familyName = "User")
    {
        var exp = (expUnixSeconds ?? DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                sub,
                email,
                email_verified = emailVerified,
                aud,
                iss,
                exp,
                name,
                given_name = givenName,
                family_name = familyName,
            }),
        };
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenProviderDisabled_ReturnsServiceUnavailable()
    {
        var settings = ValidSettings();
        settings.Enabled = false;
        var sut = CreateSut(settings, _ => throw new InvalidOperationException("No debería llamar a Google."));

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status503Unavailable);
    }

    [Theory]
    [InlineData("", "secret", "https://redirect")]
    [InlineData("client", "", "https://redirect")]
    [InlineData("client", "secret", "")]
    public async Task ExchangeCodeAsync_WhenConfigurationIncomplete_ReturnsInternalError(string clientId, string clientSecret, string redirectUri)
    {
        var settings = new GoogleAuthSettings { Enabled = true, ClientId = clientId, ClientSecret = clientSecret, RedirectUri = redirectUri };
        var sut = CreateSut(settings, _ => throw new InvalidOperationException("No debería llamar a Google."));

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenGoogleRejectsCode_ReturnsDependencyError()
    {
        var sut = CreateSut(ValidSettings(), _ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        var result = await sut.ExchangeCodeAsync("bad-code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenTokenResponseHasNoIdToken_ReturnsDependencyError()
    {
        var sut = CreateSut(ValidSettings(), request => request.Method == HttpMethod.Post
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { }) }
            : throw new InvalidOperationException("No debería llegar a tokeninfo."));

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenGoogleRejectsTokenInfo_ReturnsDependencyError()
    {
        var sut = CreateSut(ValidSettings(), request => request.Method == HttpMethod.Post
            ? TokenResponse("id-token-value")
            : new HttpResponseMessage(HttpStatusCode.BadRequest));

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenTokenInfoMissingSubOrEmail_ReturnsDependencyError()
    {
        var sut = CreateSut(ValidSettings(), request => request.Method == HttpMethod.Post
            ? TokenResponse("id-token-value")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { sub = (string?)null, email = (string?)null }) });

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenIssuerInvalid_ReturnsDependencyError()
    {
        var sut = CreateSut(ValidSettings(), request => request.Method == HttpMethod.Post
            ? TokenResponse("id-token-value")
            : TokenInfoResponse(iss: "https://evil.example.com"));

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenAudienceInvalid_ReturnsDependencyError()
    {
        var sut = CreateSut(ValidSettings(), request => request.Method == HttpMethod.Post
            ? TokenResponse("id-token-value")
            : TokenInfoResponse(aud: "someone-elses-client-id"));

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenTokenExpired_ReturnsDependencyError()
    {
        var sut = CreateSut(ValidSettings(), request => request.Method == HttpMethod.Post
            ? TokenResponse("id-token-value")
            : TokenInfoResponse(expUnixSeconds: DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()));

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WhenValid_ReturnsMappedProfile()
    {
        var sut = CreateSut(ValidSettings(), request => request.Method == HttpMethod.Post
            ? TokenResponse("id-token-value")
            : TokenInfoResponse());

        var result = await sut.ExchangeCodeAsync("code", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Provider.Should().Be(ExternalLoginProviders.Google);
        result.Data.ProviderId.Should().Be("google-sub-123");
        result.Data.Email.Should().Be("user@example.com");
        result.Data.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesClientIdAndRedirectUriAndState()
    {
        var sut = CreateSut(ValidSettings(), _ => throw new InvalidOperationException("No debería llamar a HTTP."));

        var url = sut.BuildAuthorizationUrl("state-123");

        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.Should().Contain("client_id=client-id");
        url.Should().Contain("state=state-123");
        url.Should().Contain(Uri.EscapeDataString(ValidSettings().RedirectUri));
    }

    [Fact]
    public void ProviderName_IsGoogle()
    {
        var sut = CreateSut(ValidSettings(), _ => throw new InvalidOperationException("No debería llamar a HTTP."));

        sut.ProviderName.Should().Be(ExternalLoginProviders.Google);
    }
}
