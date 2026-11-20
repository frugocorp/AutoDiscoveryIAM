// -----------------------------------------------------------------------
// <copyright file="DevUserSeeder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Seeder para usuarios de desarrollo y testing.
/// Solo se ejecuta en ambientes de desarrollo para pruebas y testing.
/// </summary>
internal static class DevUserSeeder
{
  /// <summary>
  /// Usuarios de desarrollo: (Username, Email, ArgonHash, Status, IsActive, IsDevelopment,
  /// IsSuAdmin). Credenciales: user@example.com: UserPass123! ·
  /// manager@example.com: ManagerPass123! · inactive@example.com: InactivePass123! ·
  /// test@example.com: TestPass123! · dev@example.com: AdminPass123! (IsDevelopment=true) ·
  /// su@example.com: UserPass123! (IsSuAdmin=true). <c>internal</c> (no <c>private</c>) a propósito:
  /// única fuente de verdad reusada también por <c>CustomWebApplicationFactory</c> en
  /// integration tests (vía <c>InternalsVisibleTo</c>).
  /// </summary>
  internal static readonly (string Username, string Email, string PasswordHash, string Status, bool IsActive, bool IsDevelopment, bool IsSuAdmin)[] DevUsers =
  [
    ("user", "user@example.com", "$argon2id$v=19$m=65536,t=3,p=1$EAbZ0dVvIvPEE2cf4RvOBg$aekIoccqSoBj4bPhbY2UmwSJ4nRki6TwOq3nhowvVXc", UserStatusCatalog.Active, true, false, false),
    ("manager", "manager@example.com", "$argon2id$v=19$m=65536,t=3,p=1$soHhL4p6TRLhfIcGBUDrGg$EPTmiym8wSPb7xe6438Gycc5BXWoa4r4DmzCP3bcdws", UserStatusCatalog.Active, true, false, false),
    ("inactive", "inactive@example.com", "$argon2id$v=19$m=65536,t=3,p=1$pcJKmANNkKndjtOc7z8PsQ$Eer7sl49g7uCqoj65jjk8ZsP3qrpAvw8EbSYqeIa6Q4", UserStatusCatalog.Inactive, false, false, false),
    ("test", "test@example.com", "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU", UserStatusCatalog.Active, true, false, false),
    ("dev", "dev@example.com", "$argon2id$v=19$m=65536,t=3,p=1$imBP+dxp6AfePkAmjVfl9A$Ch3Yrq6VGEnwaO2rxwAM/FjzX5bBXrrKPGseqx9O6Ig", UserStatusCatalog.Active, true, true, false),
    ("su", "su@example.com", "$argon2id$v=19$m=65536,t=3,p=1$EAbZ0dVvIvPEE2cf4RvOBg$aekIoccqSoBj4bPhbY2UmwSJ4nRki6TwOq3nhowvVXc", UserStatusCatalog.Active, true, false, true),
  ];

  /// <summary>
  /// Inserta los usuarios de desarrollo (<see cref="DevUsers"/>) en tiempo de ejecución, vía el
  /// <see cref="AutoDiscoveryIAMDbContext"/> -- agnóstico de proveedor, invocable desde
  /// <c>SeedAutoDiscoveryIAMDefaultDataAsync()</c>. Solo corre en Development/Testing.
  /// </summary>
  public static async Task SeedDataAsync(AutoDiscoveryIAMDbContext dbContext, string environmentName, CancellationToken cancellationToken = default)
  {
    if (!environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase)
      && !environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    var emails = DevUsers.Select(u => u.Email).ToArray();

    // IgnoreQueryFilters(): el índice único de Email no distingue filas borradas suavemente, así
    // que reinsertar un usuario de desarrollo ya existente (aunque esté borrado) chocaría contra la
    // restricción -- hay que detectarlo y reactivarlo en vez de reinsertarlo.
    var existingByEmail = (await dbContext.Users
      .IgnoreQueryFilters()
      .Where(u => emails.Contains(u.Email))
      .ToListAsync(cancellationToken))
      .ToDictionary(u => u.Email, StringComparer.OrdinalIgnoreCase);

    var hasChanges = false;
    foreach (var devUser in DevUsers)
    {
      if (existingByEmail.TryGetValue(devUser.Email, out var existing))
      {
        if (existing.IsDeleted)
        {
          existing.IsDeleted = false;
          hasChanges = true;
        }

        continue;
      }

      dbContext.Users.Add(new User
      {
        Username = devUser.Username,
        Email = devUser.Email,
        PasswordHash = devUser.PasswordHash,
        Status = devUser.Status,
        IsActive = devUser.IsActive,
        IsDevelopment = devUser.IsDevelopment,
        IsSuAdmin = devUser.IsSuAdmin,
      });
      hasChanges = true;
    }

    if (!hasChanges)
    {
      return;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
