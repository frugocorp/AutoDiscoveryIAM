// -----------------------------------------------------------------------
// <copyright file="CreateUserRolesOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles.Docs;

/// <summary>
/// Datos de documentación de <c>CreateUserRolesEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class CreateUserRolesOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Rol asignado o reactivado correctamente para el usuario.";

  protected override object SuccessExample { get; } = new ApiResponse<CreateUserRolesResponseDto>(
    true,
    new CreateUserRolesResponseDto(
      UserPublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
      RolePublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      AssignedAt: DateTime.Parse("2026-01-01T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };

  protected override IReadOnlyDictionary<string, ParameterDocInfo>? Parameters { get; } = new Dictionary<string, ParameterDocInfo>
  {
    ["userPublicId"] = new("PublicId del usuario al cual se asignará el rol.", "550e8400-e29b-41d4-a716-446655440000"),
    ["rolePublicId"] = new("PublicId del rol a asignar al usuario.", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
  };
}
