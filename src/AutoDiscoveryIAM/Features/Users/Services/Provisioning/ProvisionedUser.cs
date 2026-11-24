// -----------------------------------------------------------------------
// <copyright file="ProvisionedUser.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;

/// <summary>
/// Usuario creado con <see cref="IUserProvisioningService"/>.
/// </summary>
/// <param name="PublicId">Identificador público del usuario.</param>
/// <param name="Email">Correo del usuario.</param>
/// <param name="Username">Nombre de usuario.</param>
/// <param name="Status">Estado de la cuenta (<c>active</c> o <c>pending</c>).</param>
/// <param name="ActivationEmailSent">Si se envió el correo de activación.</param>
/// <param name="PasswordSetupEmailSent">Si se envió el correo para definir la contraseña.</param>
/// <param name="MustChangePassword">Si el usuario debe cambiar la contraseña inicial al entrar.</param>
public record ProvisionedUser(
    Guid PublicId,
    string Email,
    string Username,
    string Status,
    bool ActivationEmailSent,
    bool PasswordSetupEmailSent,
    bool MustChangePassword);
