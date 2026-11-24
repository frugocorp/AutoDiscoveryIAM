// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Cuerpo de <c>POST /users/by-admin</c>.
/// </summary>
/// <param name="Username">Nombre de usuario.</param>
/// <param name="Email">Correo del usuario.</param>
/// <param name="Password">
/// Contraseña inicial que define el administrador. <c>null</c>: la cuenta nace activa y el usuario
/// recibe un solo correo con un enlace para definir la suya (<paramref name="Activate"/> no aplica).
/// </param>
/// <param name="Activate">
/// <c>true</c>: la cuenta nace activa, sin correo de activación. <c>false</c>: nace pendiente y el
/// usuario recibe el correo de activación, igual que en el registro público.
/// </param>
/// <param name="MustChangePassword">
/// Obliga al usuario a cambiar la contraseña inicial en su primer inicio de sesión: hasta hacerlo solo
/// puede usar los endpoints propios de su cuenta. Con <paramref name="Password"/>, <c>null</c> equivale a
/// <c>true</c>: la contraseña que eligió el administrador no queda vigente salvo que se pida con
/// <c>false</c>. Sin <paramref name="Password"/> no aplica (solo se acepta <c>null</c> o <c>false</c>).
/// </param>
public record CreateUserByAdminRequestDto(
    string Username,
    string Email,
    string? Password,
    bool Activate,
    bool? MustChangePassword = null);
