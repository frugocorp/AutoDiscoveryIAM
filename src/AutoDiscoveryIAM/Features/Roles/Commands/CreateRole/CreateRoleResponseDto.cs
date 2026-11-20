// -----------------------------------------------------------------------
// <copyright file="CreateRoleResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;

/// <summary>
/// DTO para exponer los datos de un rol creado en la respuesta de CreateRole.
/// Solo expone el PublicId, nunca el Id interno de la base de datos.
/// </summary>
public record CreateRoleResponseDto(Guid PublicId, string Name);
