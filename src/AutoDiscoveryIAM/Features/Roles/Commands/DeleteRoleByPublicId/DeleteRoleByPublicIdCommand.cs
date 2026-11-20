// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

/// <summary>
/// Comando para eliminar lógicamente un rol por su PublicId.
/// </summary>
/// <param name="PublicId">Identificador público único del rol a eliminar.</param>
public record DeleteRoleByPublicIdCommand(Guid PublicId);