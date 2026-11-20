// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// Define la interfaz para el acceso y actualización de entidades Role en la base de datos.
/// </summary>
public interface IUpdateRoleByPublicIdRepository
{
  /// <summary>
  /// Obtiene un rol por su identificador público, excluyendo los eliminados lógicamente.
  /// </summary>
  /// <param name="publicId">Identificador público del rol a buscar.</param>
  /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
  /// <returns>Instancia de <see cref="Role"/> si existe; de lo contrario, <c>null</c>.</returns>
  Task<Role?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Obtiene un rol por su nombre, excluyendo los eliminados lógicamente.
  /// </summary>
  /// <param name="name">Nombre del rol a buscar.</param>
  /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
  /// <returns>Instancia de <see cref="Role"/> si existe; de lo contrario, <c>null</c>.</returns>
  Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

  /// <summary>
  /// Actualiza los datos de un rol existente y registra la fecha de actualización.
  /// </summary>
  /// <param name="role">Entidad <see cref="Role"/> a actualizar.</param>
  /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
  /// <returns>El rol actualizado.</returns>
  Task<Role> UpdateAsync(Role role, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación concreta de <see cref="IUpdateRoleByPublicIdRepository"/> para la gestión de roles en la base de datos.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UpdateRoleByPublicIdRepository"/> class.
/// </remarks>
/// <param name="context">Contexto de base de datos de la aplicación.</param>
public class UpdateRoleByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IUpdateRoleByPublicIdRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  /// <inheritdoc/>
  public async Task<Role?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
  {
    return await _context.Roles
        .FirstOrDefaultAsync(r => r.PublicId == publicId && !r.IsDeleted, cancellationToken);
  }

  /// <inheritdoc/>
  public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
  {
    return await _context.Roles
        .FirstOrDefaultAsync(r => r.Name == name && !r.IsDeleted, cancellationToken);
  }

  /// <inheritdoc/>
  public Task<Role> UpdateAsync(Role role, CancellationToken cancellationToken = default)
  {
    // No se asigna UpdatedAt manualmente - el AutoDiscoveryIAMDbContext lo maneja automáticamente
    // en ApplyAuditInfo() cuando se llama SaveChangesAsync()
    _context.Roles.Update(role);
    return Task.FromResult(role); // Let AutoDiscoveryIAMUnitOfWork handle SaveChangesAsync
  }
}
