// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId.Docs;

/// <summary>
/// Datos de documentación de <c>GetRoleByPublicIdEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GetRoleByPublicIdOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Rol recuperado correctamente.";

  protected override object SuccessExample { get; } = new GetRoleByPublicIdResponse(
    true,
    new GetRoleByPublicIdResponseDto(
      Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
      "Administrator",
      "Rol con acceso completo al sistema",
      true,
      new DateTime(2025, 9, 13, 10, 0, 0, 0, DateTimeKind.Utc),
      null,
      null,
      false),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };
}
