// -----------------------------------------------------------------------
// <copyright file="RegistrationSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;

/// <summary>
/// Formas en que un usuario puede crearse a sí mismo una cuenta (sección <c>Registration</c>). Cada
/// forma se prende o se apaga por separado; pueden quedar todas apagadas, y entonces solo un
/// administrador da de alta usuarios. Apagar una forma de registro nunca impide iniciar sesión a
/// quien ya tiene cuenta.
/// </summary>
public sealed class RegistrationSettings
{
  /// <summary>
  /// Nombre de la sección de configuración.
  /// </summary>
  public const string SectionName = "Registration";

  /// <summary>
  /// Registro público con correo y contraseña (<c>POST /users</c>). Default <c>true</c>.
  /// </summary>
  public bool EmailPasswordEnabled { get; set; } = true;

  /// <summary>
  /// Alta automática al entrar por primera vez con un proveedor externo, por nombre de proveedor
  /// (<c>"Google": false</c>). Un proveedor que no aparece aquí queda habilitado. Solo controla la
  /// creación de cuentas nuevas: el login de cuentas existentes depende de la configuración del
  /// proveedor (<c>Authentication:Google:Enabled</c>).
  /// </summary>
  public IDictionary<string, bool> ExternalProviders { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Si es <c>true</c>, quien se registra solo (por correo o con un proveedor externo) queda en
  /// <c>pending_approval</c> aunque confirme su correo, hasta que un administrador lo apruebe
  /// (<c>POST /users/{publicId}/approve</c>). No aplica a las altas hechas por un administrador.
  /// Default <c>false</c>.
  /// </summary>
  public bool RequireAdminApproval { get; set; }

  /// <summary>
  /// Vigencia (horas) del enlace para definir la contraseña de una cuenta que un administrador creó
  /// sin contraseña. Vencido, el usuario usa el restablecimiento normal. Default 72.
  /// </summary>
  public int PasswordSetupLinkExpiryHours { get; set; } = 72;

  /// <summary>
  /// Estado inicial de una cuenta creada por el propio usuario, según <see cref="RequireAdminApproval"/>.
  /// </summary>
  public string SelfRegistrationStatus => RequireAdminApproval ? UserStatusCatalog.PendingApproval : UserStatusCatalog.Pending;

  /// <summary>
  /// Indica si el proveedor externo puede crear cuentas nuevas.
  /// </summary>
  /// <param name="providerName">Nombre del proveedor (por ejemplo <c>Google</c>).</param>
  /// <returns><c>true</c> si no está configurado o está en <c>true</c>.</returns>
  public bool IsExternalProviderEnabled(string providerName)
    => !ExternalProviders.TryGetValue(providerName, out var enabled) || enabled;
}
