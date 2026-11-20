// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdQuery.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// Query para obtener un rol por PublicId (CQRS Minimal API).
/// Solo acepta PublicId, nunca el Id interno de la base de datos.
/// </summary>
public record GetRoleByPublicIdQuery(Guid PublicId);
