// -----------------------------------------------------------------------
// <copyright file="GoogleProvider.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google;

public class GoogleProvider(IOptions<GoogleAuthSettings> settings, IHttpClientFactory httpClientFactory) : IExternalLoginProvider
{
    private readonly GoogleAuthSettings _settings = settings.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public string ProviderName => ExternalLoginProviders.Google;

    public string BuildAuthorizationUrl(string state)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_type"] = "code",
            // "profile" no se solicita a propósito -- la librería no persiste nombre/foto de
            // Google (ver "Privacidad y datos de terceros" en el sitio de documentación), así que
            // pedir ese scope sería recolectar datos que nunca se usan. "openid email" ya cubre
            // sub/email/email_verified, lo único que se lee.
            ["scope"] = "openid email",
            ["state"] = state,
            ["access_type"] = "online",
            ["include_granted_scopes"] = "true",
            ["prompt"] = "select_account",
        };

        var encoded = string.Join("&", query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return $"https://accounts.google.com/o/oauth2/v2/auth?{encoded}";
    }

    public async Task<OperationResult<ExternalIdentityProfile>> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var validationError = ValidateConfiguration();
        if (validationError != null)
        {
            return validationError;
        }

        var client = _httpClientFactory.CreateClient();
        var tokenPayload = await RequestTokenAsync(client, code, cancellationToken);
        if (!tokenPayload.Success || tokenPayload.Data == null)
        {
            return OperationResult.Fail<ExternalIdentityProfile>(tokenPayload.ErrorInfo!, tokenPayload.Error!);
        }

        var tokenInfoResult = await ValidateTokenWithGoogleAsync(client, tokenPayload.Data.IdToken!, cancellationToken);
        if (!tokenInfoResult.Success || tokenInfoResult.Data == null)
        {
            return OperationResult.Fail<ExternalIdentityProfile>(tokenInfoResult.ErrorInfo!, tokenInfoResult.Error!);
        }

        var tokenValidationResult = ValidateTokenInfo(tokenInfoResult.Data);
        if (!tokenValidationResult.Success)
        {
            return OperationResult.Fail<ExternalIdentityProfile>(tokenValidationResult.ErrorInfo!, tokenValidationResult.Error!);
        }

        var profile = MapProfile(tokenInfoResult.Data);
        return OperationResult.Ok(profile);
    }

    private OperationResult<ExternalIdentityProfile>? ValidateConfiguration()
    {
        if (!_settings.Enabled)
        {
            return OperationResult.Fail<ExternalIdentityProfile>(ErrorInfo.Status503Unavailable, "El login con Google esta deshabilitado en la configuracion.");
        }

        if (string.IsNullOrWhiteSpace(_settings.ClientId) ||
            string.IsNullOrWhiteSpace(_settings.ClientSecret) ||
            string.IsNullOrWhiteSpace(_settings.RedirectUri))
        {
            return OperationResult.Fail<ExternalIdentityProfile>(ErrorInfo.Status500Unknown, "La configuracion de Google no esta completa.");
        }

        return null;
    }

    private async Task<OperationResult<GoogleTokenResponse>> RequestTokenAsync(HttpClient client, string code, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["redirect_uri"] = _settings.RedirectUri,
            ["grant_type"] = "authorization_code",
        };

        using var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(form), cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return OperationResult.Fail<GoogleTokenResponse>(ErrorInfo.Status424Dependency, "Google no acepto el codigo de autorizacion.");
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);
        if (tokenPayload == null || string.IsNullOrWhiteSpace(tokenPayload.IdToken))
        {
            return OperationResult.Fail<GoogleTokenResponse>(ErrorInfo.Status424Dependency, "No se recibio id_token de Google.");
        }

        return OperationResult.Ok(tokenPayload);
    }

    private static async Task<OperationResult<GoogleTokenInfoResponse>> ValidateTokenWithGoogleAsync(HttpClient client, string idToken, CancellationToken cancellationToken)
    {
        using var tokenInfoResponse = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}", cancellationToken);
        if (!tokenInfoResponse.IsSuccessStatusCode)
        {
            return OperationResult.Fail<GoogleTokenInfoResponse>(ErrorInfo.Status424Dependency, "No fue posible validar el id_token con Google.");
        }

        var tokenInfo = await tokenInfoResponse.Content.ReadFromJsonAsync<GoogleTokenInfoResponse>(cancellationToken: cancellationToken);
        if (tokenInfo == null || string.IsNullOrWhiteSpace(tokenInfo.Sub) || string.IsNullOrWhiteSpace(tokenInfo.Email))
        {
            return OperationResult.Fail<GoogleTokenInfoResponse>(ErrorInfo.Status424Dependency, "El id_token de Google no contiene los datos requeridos.");
        }

        return OperationResult.Ok(tokenInfo);
    }

    private OperationResult<bool> ValidateTokenInfo(GoogleTokenInfoResponse tokenInfo)
    {
        var issuer = tokenInfo.Iss;
        if (!string.Equals(issuer, "https://accounts.google.com", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(issuer, "accounts.google.com", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "Issuer invalido para id_token de Google.");
        }

        if (!string.Equals(tokenInfo.Aud, _settings.ClientId, StringComparison.Ordinal))
        {
            return OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "Audience invalida para id_token de Google.");
        }

        if (!long.TryParse(tokenInfo.Exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expUnix))
        {
            return OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "Expiracion invalida en id_token de Google.");
        }

        if (DateTimeOffset.FromUnixTimeSeconds(expUnix) <= DateTimeOffset.UtcNow)
        {
            return OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "El id_token de Google ya expiro.");
        }

        return OperationResult.Ok(true);
    }

    private static ExternalIdentityProfile MapProfile(GoogleTokenInfoResponse tokenInfo)
    {
        return new ExternalIdentityProfile(
          Provider: ExternalLoginProviders.Google,
          ProviderId: tokenInfo.Sub!,
          Email: tokenInfo.Email!,
          IsEmailVerified: string.Equals(tokenInfo.EmailVerified, "true", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
    }

    private sealed class GoogleTokenInfoResponse
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public string? EmailVerified { get; set; }

        [JsonPropertyName("aud")]
        public string? Aud { get; set; }

        [JsonPropertyName("iss")]
        public string? Iss { get; set; }

        [JsonPropertyName("exp")]
        public string? Exp { get; set; }
    }
}
