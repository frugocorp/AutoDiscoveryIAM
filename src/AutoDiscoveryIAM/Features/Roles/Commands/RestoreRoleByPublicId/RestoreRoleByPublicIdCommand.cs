// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

/// <summary>
/// Comando para restaurar un rol eliminado por su identificador público.
/// </summary>
/// <remarks>
/// Este comando implementa el patrón CQRS para la restauración de roles.
/// Permite reactivar roles que fueron eliminados lógicamente (soft delete).
/// </remarks>
/// <param name="PublicId">Identificador público único del rol a restaurar.</param>
public record RestoreRoleByPublicIdCommand(Guid PublicId);