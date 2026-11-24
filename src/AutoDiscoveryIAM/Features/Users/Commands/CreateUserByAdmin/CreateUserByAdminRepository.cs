// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Acceso a datos del alta de usuarios por un administrador.
/// </summary>
public interface ICreateUserByAdminRepository
{
  /// <summary>
  /// Indica si el correo ya está registrado, incluidas las cuentas borradas suavemente.
  /// </summary>
  /// <param name="email">Correo a buscar.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns><c>true</c> si existe.</returns>
  Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

  /// <summary>
  /// Indica si el nombre de usuario ya está registrado, incluidas las cuentas borradas suavemente.
  /// </summary>
  /// <param name="username">Nombre de usuario a buscar.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns><c>true</c> si existe.</returns>
  Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);

  /// <summary>
  /// Agrega el usuario al contexto, sin guardar.
  /// </summary>
  /// <param name="user">Usuario a crear.</param>
  void Add(User user);

  /// <summary>
  /// Saca del contexto un usuario agregado que no llegó a guardarse, junto con su rol por defecto.
  /// </summary>
  /// <param name="user">Usuario agregado con <see cref="Add"/>.</param>
  void Discard(User user);
}

/// <inheritdoc />
public class CreateUserByAdminRepository(AutoDiscoveryIAMDbContext context) : ICreateUserByAdminRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  /// <inheritdoc />
  public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    // IgnoreQueryFilters(): el índice único no distingue cuentas borradas suavemente.
    => _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken);

  /// <inheritdoc />
  public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
    => _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username, cancellationToken);

  /// <inheritdoc />
  public void Add(User user) => _context.Users.Add(user);

  /// <inheritdoc />
  public void Discard(User user)
  {
    ArgumentNullException.ThrowIfNull(user);
    foreach (var userRole in user.UserRoles)
    {
      _context.Entry(userRole).State = EntityState.Detached;
    }

    _context.Entry(user).State = EntityState.Detached;
  }
}
