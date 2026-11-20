// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMUpgradeRecord.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Upgrades;

/// <summary>
/// Registro de un paso de actualización de datos ya aplicado a esta base de datos (ver
/// <see cref="AutoDiscoveryIAMUpgradeRunner"/>). Existe para que cada paso corra una sola vez, aunque
/// la app reinicie o haya varias instancias.
/// </summary>
public class AutoDiscoveryIAMUpgradeRecord
{
  /// <summary>
  /// Gets or sets el identificador estable del paso (ej. <c>0.2.0-rename-users-activation</c>).
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets cuándo se aplicó (UTC).
  /// </summary>
  public DateTime AppliedAtUtc { get; set; }
}

/// <summary>
/// Configuración EF Core de <see cref="AutoDiscoveryIAMUpgradeRecord"/>.
/// </summary>
internal sealed class AutoDiscoveryIAMUpgradeRecordConfiguration : IEntityTypeConfiguration<AutoDiscoveryIAMUpgradeRecord>
{
  /// <inheritdoc />
  public void Configure(EntityTypeBuilder<AutoDiscoveryIAMUpgradeRecord> builder)
  {
    builder.ToTable("AutoDiscoveryIAMUpgrades");
    builder.HasKey(record => record.Id);
    builder.Property(record => record.Id).HasMaxLength(200);
    builder.Property(record => record.AppliedAtUtc).IsRequired();
  }
}
