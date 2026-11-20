// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// Representa la respuesta de la operación de actualización de tipo de usuario a Administrador.
/// </summary>
/// <remarks>
/// Devuelto por <see cref="UpdateUserTypeAdminHandler"/> tras procesar el comando <see cref="UpdateUserTypeAdminCommand"/>.
/// </remarks>
public record UpdateUserTypeAdminResponse : ApiResponse<UpdateUserTypeAdminResponseDto>
{
  /// <summary>
  /// Inicializa una nueva instancia de <see cref="UpdateUserTypeAdminResponse"/>.
  /// </summary>
  /// <param name="success">Indica si la actualización fue exitosa.</param>
  /// <param name="data">Datos del usuario actualizado, o <c>null</c> si la actualización falló.</param>
  /// <param name="error">Mensaje de error en caso de fallo, o <c>null</c> si la operación fue exitosa.</param>
  /// <param name="errorInfo">Objeto con metadatos estructurados del error, o <c>null</c> si la operación fue exitosa.</param>
  public UpdateUserTypeAdminResponse(
      bool success,
      UpdateUserTypeAdminResponseDto? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
  {
  }
}
