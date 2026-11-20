// -----------------------------------------------------------------------
// <copyright file="UserConfiguration.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

/// <summary>
/// Configuración EF Core para la entidad User, alineada a convenciones, seguridad y auditoría.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.HasKey(x => x.Id);
    builder.Property(x => x.PublicId).IsRequired();

    // Índice único para PublicId (CRÍTICO para seguridad)
    // Este índice permite búsquedas rápidas por PublicId sin exponer IDs internos secuenciales
    // que podrían ser vulnerables. Se usa en todas las consultas de API externa.
    builder.HasIndex(x => x.PublicId)
        .IsUnique()
        .HasDatabaseName("IX_Users_PublicId_Security");

    builder.Property(x => x.Username).IsRequired().HasMaxLength(100);
    builder.HasIndex(x => x.Username).IsUnique();

    builder.Property(x => x.Status)
        .HasConversion<string>();
    builder.Property(x => x.Email).IsRequired().HasMaxLength(150);
    builder.HasIndex(x => x.Email).IsUnique();

    builder.Property(x => x.PasswordHash).IsRequired();
    builder.Property(x => x.PasswordResetToken).HasMaxLength(256);
    builder.Property(x => x.PasswordResetTokenExpiration);
    builder.Property(x => x.EmailVerificationToken).HasMaxLength(256);
    builder.Property(x => x.EmailVerificationTokenExpiration);
    builder.Property(x => x.EmailVerificationLastSentAt);
    builder.Property(x => x.EmailVerificationResendWindowStartAt);
    builder.Property(x => x.EmailVerificationResendCount).HasDefaultValue(0);
    builder.Property(x => x.IsEmailVerified).HasDefaultValue(false);
    builder.Property(x => x.PhoneNumber).HasMaxLength(20);
    builder.Property(x => x.IsPhoneNumberVerified).HasDefaultValue(false);
    builder.Property(x => x.TwoFactorEnabled).HasDefaultValue(false);
    builder.Property(x => x.TwoFactorMethod)
        .HasMaxLength(20)
        .HasDefaultValue(TwoFactorMethodCatalog.None);
    builder.Property(x => x.TwoFactorSecret).HasMaxLength(256);
    builder.Property(x => x.TwoFactorPendingSecret).HasMaxLength(256);
    builder.Property(x => x.TwoFactorCode).HasMaxLength(16);
    builder.Property(x => x.TwoFactorCodeExpiration);
    builder.Property(x => x.TwoFactorChallengeToken).HasMaxLength(256);
    builder.Property(x => x.TwoFactorChallengeTokenExpiration);
    builder.HasIndex(x => x.TwoFactorChallengeToken);
    builder.Property(x => x.IsActive).HasDefaultValue(false);
    builder.Property(x => x.BirthOrConstitutionDate);
    builder.Property(x => x.LastLoginAt);
    builder.Property(x => x.FailedLoginAttempts).HasDefaultValue(0);
    builder.Property(x => x.LockoutEnd);

    // concurrencia optimista — SQL Server actualiza esta columna en cada UPDATE.
    builder.Property(x => x.RowVersion).IsRowVersion();

    builder.HasQueryFilter(x => !x.IsDeleted);

    // Relaciones futuras: Address, Avatar, UserProvider
    // builder.HasOne(x => x.Address).WithOne().HasForeignKey<Address>(a => a.UserId);
    // builder.HasOne(x => x.Avatar).WithOne().HasForeignKey<Avatar>(a => a.UserId);
    // builder.HasMany(x => x.UserProviders).WithOne().HasForeignKey(up => up.UserId);
  }
}
