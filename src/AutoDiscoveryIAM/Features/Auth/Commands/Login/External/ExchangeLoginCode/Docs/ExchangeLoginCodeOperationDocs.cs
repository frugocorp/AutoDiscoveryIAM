// -----------------------------------------------------------------------
// <copyright file="ExchangeLoginCodeOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>ExchangeLoginCode</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class ExchangeLoginCodeOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription =>
    "Código válido y canjeado -- queda consumido, un segundo intento con el mismo código falla con 401.";

  protected override object SuccessExample { get; } = new ApiResponse<object>(
    true,
    new LoginResponseDto(
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      DateTime.UtcNow.AddMinutes(60),
      "refresh-token-ejemplo-1234567890"),
    null,
    null);

  protected override object RequestExample { get; } = new ExchangeLoginCodeRequestDto("k7f3a1-codigo-de-un-solo-uso-9d2c4e");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
  };
}
