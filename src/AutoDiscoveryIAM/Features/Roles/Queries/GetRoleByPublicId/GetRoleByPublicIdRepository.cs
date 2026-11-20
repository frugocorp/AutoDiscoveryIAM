// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// Interfaz para obtener un rol por PublicId.
/// </summary>
public interface IGetRoleByPublicIdRepository
{
    Task<Role?> GetRoleByPublicId(Guid publicId);
}

/// <summary>
/// Implementación del repositorio para obtener un rol por PublicId.
/// Solo busca por PublicId, nunca por Id interno.
/// </summary>
public class GetRoleByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetRoleByPublicIdRepository
{
    public async Task<Role?> GetRoleByPublicId(Guid publicId)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.PublicId == publicId && !r.IsDeleted);
    }
}
