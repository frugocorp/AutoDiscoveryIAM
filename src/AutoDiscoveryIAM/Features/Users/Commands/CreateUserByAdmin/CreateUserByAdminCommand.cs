// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Alta de un usuario hecha por un administrador.
/// </summary>
/// <param name="Username">Nombre de usuario.</param>
/// <param name="Email">Correo del usuario.</param>
/// <param name="Password">Contraseña inicial, o <c>null</c> para invitar al usuario a definirla.</param>
/// <param name="Activate">Si la cuenta nace activa (sin correo) o pendiente de activación.</param>
/// <param name="MustChangePassword">
/// Si el usuario debe cambiar la contraseña inicial al entrar; <c>null</c> equivale a <c>true</c> cuando hay
/// <paramref name="Password"/>.
/// </param>
public record CreateUserByAdminCommand(
    string Username,
    string Email,
    string? Password,
    bool Activate,
    bool? MustChangePassword = null);
