// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// Define la interfaz para el acceso y actualización de tipo Admin de entidades User en la base de datos.
/// </summary>
public interface IUpdateUserTypeAdminRepository
{
  /// <summary>
  /// Obtiene un usuario por su identificador público, excluyendo los eliminados lógicamente.
  /// </summary>
  /// <param name="publicId">Identificador público del usuario a buscar.</param>
  /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
  /// <returns>Instancia de <see cref="User"/> si existe; de lo contrario, <c>null</c>.</returns>
  Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Actualiza el tipo Admin del usuario en la base de datos.
  /// </summary>
  /// <param name="user">Usuario a actualizar.</param>
  /// <param name="cancellationToken">Token de cancelación para la operación asíncrona.</param>
  Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación del repositorio para cambio de tipo Admin de usuarios usando Entity Framework.
/// </summary>
/// <param name="context">Contexto de base de datos.</param>
public class UpdateUserTypeAdminRepository(AutoDiscoveryIAMDbContext context) : IUpdateUserTypeAdminRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  /// <inheritdoc />
  public async Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
  {
    return await _context.Users
        .Where(u => u.PublicId == publicId && !u.IsDeleted)
        .FirstOrDefaultAsync(cancellationToken);
  }

  /// <inheritdoc />
  public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
  {
    _context.Users.Update(user);
    await Task.CompletedTask;
  }
}
