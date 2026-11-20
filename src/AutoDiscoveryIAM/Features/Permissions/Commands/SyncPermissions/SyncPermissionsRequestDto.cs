// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// DTO de request para la sincronización de permisos.
/// </summary>
/// <remarks>
/// Contiene los parámetros de configuración para el proceso de sincronización.
/// </remarks>
/// <param name="ForceFullSync">Si es true, fuerza una sincronización completa ignorando cache.</param>
/// <param name="DryRun">Si es true, ejecuta la sincronización sin persistir cambios (solo reporte).</param>
/// <param name="EnableDetailedConsoleOutput">Si es true, imprime en consola cada permiso descubierto con su estado.</param>
public record SyncPermissionsRequestDto(
    bool ForceFullSync = false,
    bool DryRun = false,
    bool EnableDetailedConsoleOutput = false);
