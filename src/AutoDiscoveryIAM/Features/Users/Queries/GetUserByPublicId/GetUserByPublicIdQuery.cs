// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdQuery.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

/// <summary>
/// Query para obtener un usuario por PublicId. Solo acepta PublicId, nunca el Id
/// interno de la base de datos.
/// </summary>
public record GetUserByPublicIdQuery(Guid PublicId);
