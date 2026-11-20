// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

public interface IRestoreRoleByPublicIdRepository
{
  Task<Role?> GetDeletedRoleByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
  Task<bool> ExistsActiveRoleWithNameAsync(string name, CancellationToken cancellationToken = default);
  Task<bool> RestoreRoleAsync(Role role, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repositorio para la restauración de roles por PublicId.
/// </summary>
/// <remarks>
/// Proporciona acceso a datos específico para la operación de restauración de roles,
/// manejando la lógica de consulta y actualización de estado de eliminación.
/// </remarks>
public class RestoreRoleByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IRestoreRoleByPublicIdRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  /// <summary>
  /// Busca un rol eliminado por su PublicId.
  /// </summary>
  /// <param name="publicId">Identificador público del rol.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>El rol eliminado si existe, null en caso contrario.</returns>
  public async Task<Role?> GetDeletedRoleByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
  {
    return await _context.Roles
        .IgnoreQueryFilters() // Ignorar el filtro de soft delete para ver roles eliminados
        .Where(r => r.PublicId == publicId && r.IsDeleted)
        .FirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Verifica si existe un rol activo con el mismo nombre.
  /// </summary>
  /// <param name="name">Nombre del rol a verificar.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>True si existe un rol activo con ese nombre, false en caso contrario.</returns>
  public async Task<bool> ExistsActiveRoleWithNameAsync(string name, CancellationToken cancellationToken = default)
  {
    return await _context.Roles
        .AnyAsync(r => r.Name == name && !r.IsDeleted, cancellationToken);
  }

  /// <summary>
  /// Restaura un rol eliminado.
  /// </summary>
  /// <param name="role">Rol a restaurar.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>True si la restauración fue exitosa.</returns>
  /// <remarks>
  /// Sin try/catch a propósito: cualquier excepción debe propagarse hasta
  /// <c>ExceptionHandlingMiddleware</c>, no tragarse en silencio.
  /// </remarks>
  public Task<bool> RestoreRoleAsync(Role role, CancellationToken cancellationToken = default)
  {
    // Solo cambiar IsDeleted - Los campos RestoredAt/RestoredBy se manejan automáticamente en AutoDiscoveryIAMDbContext
    // Los campos DeletedAt/DeletedBy se mantienen para preservar el historial de auditoría completo
    role.IsDeleted = false;

    _context.Roles.Update(role);
    // No hacer SaveChanges aquí, se maneja desde el AutoDiscoveryIAMUnitOfWork
    return Task.FromResult(true);
  }
}