// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsCommandResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// DTO de respuesta del handler para la sincronización de permisos.
/// </summary>
/// <remarks>
/// Contiene únicamente el resultado del servicio orquestador, manteniendo la separación
/// de responsabilidades y simplificando la interfaz del handler.
/// </remarks>
/// <param name="OrchestrationResult">Resultado consolidado del proceso de sincronización.</param>
public record SyncPermissionsCommandResponseDto(
    AnalyzeUpdatedPermissionsResponseDto OrchestrationResult);
