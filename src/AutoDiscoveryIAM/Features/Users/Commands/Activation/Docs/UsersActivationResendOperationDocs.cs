// -----------------------------------------------------------------------
// <copyright file="UsersActivationResendOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>UsersActivationResend</c> (<c>ActivationEndPoint</c>) para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class UsersActivationResendOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ActivationResponse(
    true,
    new ActivationStatusDto(
      "usuario@example.com",
      true,
      "Cuenta activada correctamente.",
      "https://api.autodiscoveryiam.com/login"));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>();
}
