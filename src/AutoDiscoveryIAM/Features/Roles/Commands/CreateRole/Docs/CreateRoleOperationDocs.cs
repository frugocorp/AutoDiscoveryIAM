// -----------------------------------------------------------------------
// <copyright file="CreateRoleOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole.Docs;

/// <summary>
/// Datos de documentación de <c>CreateRoleEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class CreateRoleOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status201Created;

  protected override string SuccessDescription =>
    "Rol creado. La respuesta incluye los datos guardados, incluido el PublicId.";

  protected override object SuccessExample { get; } = new ApiResponse<CreateRoleResponseDto>(
    true,
    new CreateRoleResponseDto(Guid.Parse("550e8400-e29b-41d4-a716-446655440005"), "Moderador"),
    null,
    null);

  protected override object RequestExample { get; } = new CreateRoleCommand(
    Name: "Moderador",
    Description: "Rol de moderador con permisos limitados de administración.",
    IsActive: true);

  protected override SuccessLinkInfo? SuccessLink { get; } = new(
    LinkedOperationId: "GetRoleByPublicId",
    Description: "Consulta el rol creado usando su PublicId.",
    ParameterName: "publicId",
    Expression: "$response.body#/data/publicId");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };
}
