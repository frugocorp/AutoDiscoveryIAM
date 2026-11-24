// -----------------------------------------------------------------------
// <copyright file="IDefaultRoleAssignmentService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;

/// <summary>
/// Asigna a un usuario recién creado el rol marcado con
/// <see cref="Roles.Models.Entities.Role.IsDefaultForNewUser"/>. Lo usan todos los caminos que crean
/// usuarios (registro por correo, alta por proveedor externo, alta por un administrador).
/// </summary>
public interface IDefaultRoleAssignmentService
{
  /// <summary>
  /// Agrega al usuario la relación con el rol por defecto, si existe uno activo. No guarda: la
  /// relación se persiste en el mismo <c>SaveChangesAsync</c> que crea al usuario, así ambos quedan
  /// en la misma transacción.
  /// </summary>
  /// <param name="user">Usuario recién agregado al contexto (todavía sin guardar).</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns><c>true</c> si se asignó el rol; <c>false</c> si no hay un rol por defecto activo.</returns>
  Task<bool> AssignDefaultRoleAsync(User user, CancellationToken cancellationToken);
}
