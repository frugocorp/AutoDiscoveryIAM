// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

/// <summary>
/// Define la interfaz para el acceso y eliminación lógica de entidades Role en la base de datos.
/// </summary>
public interface IDeleteRoleByPublicIdRepository
{
    /// <summary>
    /// Obtiene un rol por su identificador público, excluyendo los eliminados lógicamente.
    /// </summary>
    /// <param name="publicId">Identificador público del rol a buscar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    /// <returns>Instancia de <see cref="Role"/> si existe; de lo contrario, <c>null</c>.</returns>
    Task<Role?> GetRoleByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un rol tiene usuarios asignados activos.
    /// </summary>
    /// <param name="roleId">ID interno del rol a verificar.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    /// <returns>True si el rol tiene usuarios asignados; de lo contrario, false.</returns>
    Task<bool> HasActiveUsersAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina lógicamente un rol existente marcándolo como eliminado.
    /// </summary>
    /// <param name="role">Entidad <see cref="Role"/> a eliminar lógicamente.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
    /// <returns>True si la operación fue exitosa.</returns>
    Task<bool> DeleteRoleAsync(Role role, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación concreta de <see cref="IDeleteRoleByPublicIdRepository"/> para la gestión de eliminación de roles.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DeleteRoleByPublicIdRepository"/> class.
/// </remarks>
/// <param name="context">Contexto de base de datos de la aplicación.</param>
public class DeleteRoleByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IDeleteRoleByPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    /// <inheritdoc/>
    public async Task<Role?> GetRoleByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.PublicId == publicId && !r.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasActiveUsersAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AnyAsync(ur => ur.RoleId == roleId && !ur.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        // No se asignan campos auditables manualmente - el AutoDiscoveryIAMDbContext lo maneja automáticamente
        // en ApplyAuditInfo() cuando se llama SaveChangesAsync() y detecta EntityState.Deleted
        _context.Roles.Remove(role); // Esto disparará soft delete automático
        return Task.FromResult(true); // La AutoDiscoveryIAMUnitOfWork manejará el SaveChangesAsync
    }
}