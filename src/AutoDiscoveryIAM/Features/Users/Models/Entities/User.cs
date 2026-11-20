// -----------------------------------------------------------------------
// <copyright file="User.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

public class User : AuditableEntity
{
  public long Id { get; set; }
  public string Username { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string PasswordHash { get; set; } = string.Empty;
  public string? PasswordResetToken { get; set; }
  public DateTime? PasswordResetTokenExpiration { get; set; }
  public string? EmailVerificationToken { get; set; }
  public DateTime? EmailVerificationTokenExpiration { get; set; }
  public DateTime? EmailVerificationLastSentAt { get; set; }
  public DateTime? EmailVerificationResendWindowStartAt { get; set; }
  public int EmailVerificationResendCount { get; set; } = 0;
  public bool IsEmailVerified { get; set; } = false;
  public string? PhoneNumber { get; set; }
  public bool IsPhoneNumberVerified { get; set; } = false;
  public bool TwoFactorEnabled { get; set; } = false;
  public string TwoFactorMethod { get; set; } = TwoFactorMethodCatalog.None;
  public string? TwoFactorSecret { get; set; }
  public string? TwoFactorPendingSecret { get; set; }

  /// <summary>
  /// Paso de tiempo (ventanas de 30s desde Unix epoch) del último código de Authenticator aceptado:
  /// un código con un paso igual o anterior se rechaza, aunque siga dentro de su ventana de validez.
  /// </summary>
  public long? TwoFactorLastUsedTimeStep { get; set; }
  public string? TwoFactorCode { get; set; }
  public DateTime? TwoFactorCodeExpiration { get; set; }
  public string? TwoFactorChallengeToken { get; set; }
  public DateTime? TwoFactorChallengeTokenExpiration { get; set; }
  // PhoneNumber se queda aqui a proposito (2FA por SMS a futuro), aunque tampoco se pide en el
  // registro (CreateUserRequestDto) por ahora. PersonType/FirstName/LastName/MiddleName se
  // eliminaron de este modelo -- ese tipo de dato de perfil vive en un perfil de usuario propio
  // del consumidor (ej. UserProfile, ver samples/SimpleCode/Features/UserProfiles), no en User.
  public string Status { get; set; } = UserStatusCatalog.Pending;
  public bool IsActive { get; set; } = false;

  // Campos de usuarios especiales
  public bool IsAdmin { get; set; } = false;
  public bool IsDevelopment { get; set; } = false;
  public bool IsSuAdmin { get; set; } = false;

  public DateTime? BirthOrConstitutionDate { get; set; }
  public DateTime? LastLoginAt { get; set; }
  public int FailedLoginAttempts { get; set; } = 0;
  public DateTime? LockoutEnd { get; set; }

  /// <summary>
  /// Marca de tiempo (horario de México, GMT-6) a partir de la cual un access token de este
  /// usuario se considera válido. Cualquier JWT emitido antes de este valor se rechaza, sin
  /// importar que su firma y expiración natural sigan siendo válidas. Se actualiza al cambiar o
  /// resetear la contraseña, invalidando de inmediato todas las sesiones activas en cualquier
  /// dispositivo.
  /// </summary>
  public DateTime TokenValidFrom { get; set; } = TimeHelpers.GetCurrentMexicoTime();

  /// <summary>
  /// Token de concurrencia optimista — SQL Server lo actualiza automáticamente en
  /// cada UPDATE; un update concurrente sobre una fila desactualizada falla con 409 en vez de
  /// pisar silenciosamente los cambios de otro usuario.
  /// </summary>
  public byte[] RowVersion { get; set; } = Array.Empty<byte>();

  // Relaciones RBAC
  public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
  public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

  // Relaciones futuras: Address, Avatar, UserProvider
  // public Address? Address { get; set; }
  // public Avatar? Avatar { get; set; }
  // public ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
}
