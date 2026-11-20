// -----------------------------------------------------------------------
// <copyright file="UsersActivationConfirmPostOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>UsersActivationConfirmPost</c> (<c>ActivationEndPoint</c>) para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class UsersActivationConfirmPostOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ActivationResponse(
    true,
    new ActivationStatusDto(
      "usuario@example.com",
      true,
      "Cuenta activada correctamente.",
      "https://api.autodiscoveryiam.com/login"));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
  };
}
