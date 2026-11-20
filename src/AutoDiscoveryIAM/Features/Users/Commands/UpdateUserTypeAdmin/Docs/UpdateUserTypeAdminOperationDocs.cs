// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin.Docs;

public sealed class UpdateUserTypeAdminOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ApiResponse<UpdateUserTypeAdminResponseDto>(
    true,
    new UpdateUserTypeAdminResponseDto(
      PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
      Username: "usuario.ejemplo",
      Email: "usuario@example.com",
      IsAdmin: true,
      Reason: "Usuario promovido por necesidades administrativas del proyecto"),
    null,
    null);

  protected override object RequestExample { get; } = new UpdateUserTypeAdminBodyDto(
    IsAdmin: true,
    Reason: "Usuario promovido por necesidades administrativas del proyecto");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.SuperAdminAccessRequired.StatusCode] = ErrorInfo.SuperAdminAccessRequired,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };

  protected override IReadOnlyDictionary<string, ParameterDocInfo>? Parameters { get; } = new Dictionary<string, ParameterDocInfo>
  {
    ["publicId"] = new("PublicId (GUID) del usuario cuyo tipo Administrador se modifica.", "550e8400-e29b-41d4-a716-446655440000"),
  };
}
