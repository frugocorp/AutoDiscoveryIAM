// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId.Docs;

/// <summary>
/// Datos de documentación de <c>DeleteRoleByPublicIdEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class DeleteRoleByPublicIdOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Rol eliminado (borrado lógico).";

  protected override object SuccessExample { get; } = new ApiResponse<DeleteRoleByPublicIdResponseDto>(
    true,
    new DeleteRoleByPublicIdResponseDto
    {
      PublicId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
      Status = "Eliminado",
    },
    null,
    null);

  protected override object RequestExample { get; } = new DeleteRoleByPublicIdRequestDto(
    PublicId: Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

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
