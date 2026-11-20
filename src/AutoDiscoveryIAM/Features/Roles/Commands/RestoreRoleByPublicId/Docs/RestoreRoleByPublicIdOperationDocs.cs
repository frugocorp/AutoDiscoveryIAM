// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId.Docs;

/// <summary>
/// Datos de documentación de <c>RestoreRoleByPublicIdEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class RestoreRoleByPublicIdOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Rol restaurado (borrado lógico revertido).";

  protected override object SuccessExample { get; } = new ApiResponse<RestoreRoleByPublicIdResponseDto>(
    true,
    new RestoreRoleByPublicIdResponseDto
    {
      PublicId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
      Name = "Admin",
      Status = "Restaurado",
      RestoredAt = DateTime.UtcNow,
    },
    null,
    null);

  protected override object RequestExample { get; } = new RestoreRoleByPublicIdRequestDto(
    PublicId: new Guid("a1b2c3d4-e5f6-7890-1234-567890abcdef"));

  protected override SuccessLinkInfo? SuccessLink { get; } = new(
    LinkedOperationId: "GetRoleByPublicId",
    Description: "Consulta el detalle del rol restaurado usando su PublicId.",
    ParameterName: "publicId",
    Expression: "$response.body#/data/publicId");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };
}
