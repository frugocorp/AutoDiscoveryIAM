// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// Comando para actualizar un rol existente mediante su PublicId.
/// </summary>
/// <remarks>
/// Este comando encapsula los datos necesarios para actualizar un rol, siguiendo el patrón CQRS.
/// Utilizado por <see cref="UpdateRoleByPublicIdHandler"/>.
/// </remarks>
/// <example>
/// var command = new UpdateRoleByPublicIdCommand(Guid.Parse("..."), new UpdateRoleByPublicIdBodyDto("Admin", "Rol de administrador"));
/// </example>
/// <param name="PublicId">PublicId del rol a actualizar.</param>
/// <param name="Request">Datos actualizados del rol. Validados por <see cref="UpdateRoleByPublicIdCommandValidator"/> y <see cref="RoleValidationRules"/>.</param>
public record UpdateRoleByPublicIdCommand(Guid PublicId, UpdateRoleByPublicIdBodyDto Request);
