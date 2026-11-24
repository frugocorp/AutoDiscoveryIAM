// -----------------------------------------------------------------------
// <copyright file="ProvisionUserRequest.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;

/// <summary>
/// Datos de un alta hecha con <see cref="IUserProvisioningService"/>. Mismo significado que el cuerpo
/// de <c>POST /users/by-admin</c>.
/// </summary>
/// <param name="Username">Nombre de usuario.</param>
/// <param name="Email">Correo del usuario.</param>
/// <param name="Password">
/// Contraseña inicial, o <c>null</c> para que la cuenta nazca activa y el usuario reciba un correo
/// para definir la suya.
/// </param>
/// <param name="Activate">
/// Con <paramref name="Password"/>: <c>true</c> la cuenta nace activa y no se envía correo;
/// <c>false</c> nace pendiente y el usuario recibe el correo de activación.
/// </param>
/// <param name="MustChangePassword">
/// Si el usuario debe cambiar la contraseña inicial al entrar; <c>null</c> equivale a <c>true</c>.
/// Solo aplica con <paramref name="Password"/>.
/// </param>
public record ProvisionUserRequest(
    string Username,
    string Email,
    string? Password,
    bool Activate,
    bool? MustChangePassword = null);
