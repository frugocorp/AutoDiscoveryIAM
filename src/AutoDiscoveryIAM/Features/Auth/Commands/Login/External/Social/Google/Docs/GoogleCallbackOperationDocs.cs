// -----------------------------------------------------------------------
// <copyright file="GoogleCallbackOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>GoogleCallback</c> (<c>GoogleAuthEndpoints</c>) para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GoogleCallbackOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription =>
    "Login completado. Responde este JSON solo si Authentication:ExternalLoginRedirectUri NO esta " +
    "configurado -- si lo esta, en vez de JSON el endpoint redirige (302) a esa URL: en exito con " +
    "un codigo de un solo uso (#code=...) que la SPA canjea de inmediato en " +
    "POST /auth/external-login/exchange (el token nunca toca la URL), o con el detalle del error " +
    "en el fragmento (#error=...&errorDescription=...) si falla.";

  protected override object SuccessExample { get; } = new ApiResponse<object>(
    true,
    new LoginResponseDto(
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      DateTime.UtcNow.AddMinutes(60),
      "refresh-token-ejemplo-1234567890"),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status424Dependency.StatusCode] = ErrorInfo.Status424Dependency,
  };
}
