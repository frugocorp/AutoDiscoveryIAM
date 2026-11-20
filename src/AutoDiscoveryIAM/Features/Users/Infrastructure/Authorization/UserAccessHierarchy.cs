// -----------------------------------------------------------------------
// <copyright file="UserAccessHierarchy.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Authorization;

/// <summary>
/// Jerarquía entre los flags de <see cref="User"/> para las operaciones administrativas sobre otra
/// cuenta (editarla, activarla o rechazarla, reenviarle la activación, cambiar su tipo Admin).
/// </summary>
/// <remarks>
/// El permiso RBAC de la operación no basta cuando la cuenta destino tiene flags: un administrador por
/// rol podría, por ejemplo, desactivar a un <c>IsSuAdmin</c> o cambiarle el email y tomar la cuenta con
/// el reseteo público de contraseña. Reglas: una cuenta <c>IsDevelopment</c> solo la administra otro
/// <c>IsDevelopment</c>; una <c>IsSuAdmin</c> o <c>IsAdmin</c>, un <c>IsSuAdmin</c> o <c>IsDevelopment</c>;
/// una cuenta sin flags, cualquiera con el permiso.
/// </remarks>
public static class UserAccessHierarchy
{
  /// <summary>Mensaje del 403 cuando el llamador no alcanza el nivel de la cuenta destino.</summary>
  public const string InsufficientLevelMessage = "No tienes el nivel de acceso necesario para administrar esta cuenta.";

  /// <summary>
  /// Indica si <paramref name="caller"/> puede administrar <paramref name="target"/>.
  /// </summary>
  public static bool CanManage(User? caller, User target)
  {
    ArgumentNullException.ThrowIfNull(target);

    if (!target.IsDevelopment && !target.IsSuAdmin && !target.IsAdmin)
    {
      return true;
    }

    if (target.IsDevelopment)
    {
      return caller?.IsDevelopment == true;
    }

    return caller?.IsDevelopment == true || caller?.IsSuAdmin == true;
  }

  /// <summary>
  /// Igual que <see cref="CanManage(User?, User)"/>, resolviendo el llamador desde el contexto de la
  /// petición (con consulta a la base de datos solo si la cuenta destino tiene flags).
  /// </summary>
  public static async Task<bool> CanManageAsync(IAuthContextService authContextService, User target)
  {
    ArgumentNullException.ThrowIfNull(authContextService);
    ArgumentNullException.ThrowIfNull(target);

    if (!target.IsDevelopment && !target.IsSuAdmin && !target.IsAdmin)
    {
      return true;
    }

    return CanManage(await authContextService.GetCurrentUserAsync(), target);
  }
}
