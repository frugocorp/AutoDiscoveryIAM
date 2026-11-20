// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Comando para sincronizar permisos automáticamente automáticamente.
/// </summary>
/// <remarks>
/// Este comando ejecuta el proceso completo de descubrimiento y sincronización de permisos
/// basado en endpoints existentes en el sistema, siguiendo el patrón CQRS.
/// Utilizado por <see cref="SyncPermissionsHandler"/>.
/// </remarks>
/// <example>
/// var command = new SyncPermissionsCommand(new SyncPermissionsRequestDto(true));
/// </example>
/// <param name="Request">Datos de configuración para la sincronización. Validados por <see cref="SyncPermissionsCommandValidator"/>.</param>
public record SyncPermissionsCommand(SyncPermissionsRequestDto Request);
