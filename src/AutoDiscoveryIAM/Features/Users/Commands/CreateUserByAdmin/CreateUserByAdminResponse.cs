// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Respuesta de <c>POST /users/by-admin</c>.
/// </summary>
public record CreateUserByAdminResponse : ApiResponse<CreateUserByAdminResponseDto>
{
  /// <summary>
  /// Inicializa una nueva instancia de <see cref="CreateUserByAdminResponse"/>.
  /// </summary>
  /// <param name="success">Indica si el alta fue exitosa.</param>
  /// <param name="data">Datos del usuario creado, o <c>null</c> si el alta falló.</param>
  /// <param name="error">Mensaje de error, o <c>null</c> si la operación fue exitosa.</param>
  /// <param name="errorInfo">Metadatos estructurados del error, o <c>null</c> si la operación fue exitosa.</param>
  public CreateUserByAdminResponse(
      bool success,
      CreateUserByAdminResponseDto? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
  {
  }
}
