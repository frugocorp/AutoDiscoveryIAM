// -----------------------------------------------------------------------
// <copyright file="UpdateUserRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

/// <summary>
/// Interfaz del repositorio para operaciones de actualización de usuarios.
/// </summary>
public interface IUpdateUserRepository
{
  /// <summary>
  /// Busca un usuario por su identificador público.
  /// </summary>
  /// <param name="publicId">Identificador público del usuario.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>El usuario encontrado o null si no existe.</returns>
  Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Verifica si ya existe un usuario con el username especificado (excluyendo el usuario actual).
  /// </summary>
  /// <param name="username">Username a verificar.</param>
  /// <param name="excludeUserId">ID del usuario a excluir de la búsqueda.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>True si existe otro usuario con ese username.</returns>
  Task<bool> ExistsUsernameAsync(string username, long excludeUserId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Verifica si ya existe un usuario con el email especificado (excluyendo el usuario actual).
  /// </summary>
  /// <param name="email">Email a verificar.</param>
  /// <param name="excludeUserId">ID del usuario a excluir de la búsqueda.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>True si existe otro usuario con ese email.</returns>
  Task<bool> ExistsEmailAsync(string email, long excludeUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repositorio para operaciones de actualización de usuarios.
/// </summary>
/// <remarks>
/// Implementa las operaciones de base de datos necesarias para actualizar usuarios.
/// </remarks>
public class UpdateUserRepository(AutoDiscoveryIAMDbContext context) : IUpdateUserRepository
{

  /// <inheritdoc/>
  public async Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
  {
    return await context.Users
        .Where(u => u.PublicId == publicId && !u.IsDeleted)
        .FirstOrDefaultAsync(cancellationToken);
  }

  /// <inheritdoc/>
  public async Task<bool> ExistsUsernameAsync(string username, long excludeUserId, CancellationToken cancellationToken = default)
  {
    // IgnoreQueryFilters() y sin "!IsDeleted": el índice único también cuenta cuentas borradas suavemente.
    return await context.Users
        .IgnoreQueryFilters()
        .Where(u => u.Username == username && u.Id != excludeUserId)
        .AnyAsync(cancellationToken);
  }

  /// <inheritdoc/>
  public async Task<bool> ExistsEmailAsync(string email, long excludeUserId, CancellationToken cancellationToken = default)
  {
    // IgnoreQueryFilters() y sin "!IsDeleted": el índice único también cuenta cuentas borradas suavemente.
    return await context.Users
        .IgnoreQueryFilters()
        .Where(u => u.Email == email && u.Id != excludeUserId)
        .AnyAsync(cancellationToken);
  }
}
