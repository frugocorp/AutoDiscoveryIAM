// -----------------------------------------------------------------------
// <copyright file="LoginOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.Docs;

/// <summary>
/// Datos de documentación de <c>LoginEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class LoginOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription =>
    "Login exitoso: token JWT emitido, o challenge de 2FA pendiente de verificar.";

  protected override object SuccessExample { get; } = new ApiResponse<object>(
    true,
    new LoginResponseDto(
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      DateTime.UtcNow.AddMinutes(60),
      "refresh-token-ejemplo-1234567890"),
    null,
    null);

  protected override object RequestExample { get; } = new LoginRequestDto(
    Email: "admin@example.com",
    Password: "AdminPass123!");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
  };
}
