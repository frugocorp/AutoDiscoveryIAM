// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

/// <summary>
/// Respuesta estructurada para la operación de eliminación de rol por PublicId.
/// </summary>
/// <remarks>
/// Encapsula el resultado de la eliminación incluyendo datos, errores y códigos de estado apropiados.
/// Sigue el patrón de respuesta estándar del proyecto para consistency.
/// </remarks>
public record DeleteRoleByPublicIdResponse : ApiResponse<DeleteRoleByPublicIdResponseDto>
{
    public DeleteRoleByPublicIdResponse(bool success, DeleteRoleByPublicIdResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}