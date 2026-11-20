// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount.Docs;

/// <summary>
/// Datos de documentación de <c>DeleteMyAccountEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class DeleteMyAccountOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new DeleteMyAccountResponse(
      true,
      new DeleteMyAccountResponseDto(Guid.Parse("550e8400-e29b-41d4-a716-446655440001"), "Eliminado"));

  protected override string? RequestBodyDescription => "Contraseña actual del usuario, para confirmar la eliminación.";

  protected override object? RequestExample { get; } = new DeleteMyAccountRequestDto("MiContrasenaActual123*");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };
}
