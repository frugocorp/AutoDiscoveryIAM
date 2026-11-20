// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// Request DTO para actualizar un rol mediante su PublicId.
/// </summary>
/// <remarks>
/// Este DTO unifica los parámetros de ruta y cuerpo de la solicitud HTTP.
/// Utiliza el patrón [AsParameters] para el binding automático.
/// </remarks>
/// <example>
/// var requestDto = new UpdateRoleByPublicIdRequestDto
/// {
///     PublicId = Guid.Parse("..."),
///     Body = new UpdateRoleByPublicIdBodyDto("Admin", "Rol de administrador")
/// };
/// </example>
public record UpdateRoleByPublicIdRequestDto
{
  /// <summary>
  /// Obtiene o establece el PublicId del rol a actualizar.
  /// </summary>
  /// <remarks>
  /// Se obtiene de la ruta HTTP mediante [FromRoute].
  /// </remarks>
  [FromRoute]
  public required Guid PublicId { get; init; }

  /// <summary>
  /// Obtiene o establece los datos actualizados del rol.
  /// </summary>
  /// <remarks>
  /// Se obtiene del cuerpo HTTP mediante [FromBody].
  /// </remarks>
  [FromBody]
  public required UpdateRoleByPublicIdBodyDto Body { get; init; }
}
