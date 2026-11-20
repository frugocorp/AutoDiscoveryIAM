// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// Representa la respuesta de la operación de actualización de un rol.
/// </summary>
/// <remarks>
/// Devuelto por <see cref="UpdateRoleByPublicIdHandler"/> tras procesar el comando <see cref="UpdateRoleByPublicIdCommand"/>.
/// </remarks>
public record UpdateRoleByPublicIdResponse : ApiResponse<UpdateRoleByPublicIdResponseDto>
{
  /// <summary>
  /// Inicializa una nueva instancia de <see cref="UpdateRoleByPublicIdResponse"/>.
  /// </summary>
  /// <param name="success">Indica si la actualización fue exitosa.</param>
  /// <param name="data">Datos del rol actualizado, o <c>null</c> si la actualización falló.</param>
  /// <param name="error">Mensaje de error en caso de fallo, o <c>null</c> si la operación fue exitosa.</param>
  /// <param name="errorInfo">Objeto con metadatos estructurados del error, o <c>null</c> si la operación fue exitosa.</param>
  public UpdateRoleByPublicIdResponse(
      bool success,
      UpdateRoleByPublicIdResponseDto? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
