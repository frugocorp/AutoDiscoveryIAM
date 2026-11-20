using System;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;

/// <summary>
/// Clase base para entidades auditables. Provee campos estándar para trazabilidad y auditoría.
/// </summary>
public abstract class AuditableEntity
{
  public Guid PublicId { get; set; } = Guid.NewGuid();
  public DateTime CreatedAt { get; set; }
  public long? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public long? UpdatedBy { get; set; }
  public DateTime? DeletedAt { get; set; }
  public long? DeletedBy { get; set; }
  public DateTime? RestoredAt { get; set; }
  public long? RestoredBy { get; set; }
  public bool IsDeleted { get; set; }
}

