// -----------------------------------------------------------------------
// <copyright file="EmailChangeRequest.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

/// <summary>
/// Solicitud de cambio de email: el email es el username, por lo que no es
/// autoservicio directo. El usuario solicita → un Admin revisa (aprueba para ejecución o
/// rechaza) → solo un SuAdmin ejecuta el cambio real sobre <see cref="User.Email"/>.
/// </summary>
public class EmailChangeRequest : AuditableEntity
{
  public long Id { get; set; }

  /// <summary>
  /// Usuario que solicita el cambio (y sobre el que se aplicará si se ejecuta).
  /// </summary>
  public long UserId { get; set; }

  /// <summary>
  /// Nuevo email solicitado.
  /// </summary>
  public string RequestedEmail { get; set; } = string.Empty;

  /// <summary>
  /// Estado actual del flujo (<see cref="EmailChangeRequestStatusCatalog"/>).
  /// </summary>
  public string Status { get; set; } = EmailChangeRequestStatusCatalog.Pending;

  /// <summary>
  /// Id del Admin que revisó la solicitud (aprobó para ejecución o rechazó).
  /// </summary>
  public long? ReviewedBy { get; set; }

  public DateTime? ReviewedAt { get; set; }

  /// <summary>
  /// Motivo del rechazo, solo aplica cuando <see cref="Status"/> es <c>rejected</c>.
  /// </summary>
  public string? RejectionReason { get; set; }

  /// <summary>
  /// Id del SuAdmin que ejecutó el cambio real de email.
  /// </summary>
  public long? ExecutedBy { get; set; }

  public DateTime? ExecutedAt { get; set; }

  /// <summary>
  /// Token de concurrencia optimista — evita que dos revisiones/ejecuciones
  /// concurrentes sobre la misma solicitud (Admin y SuAdmin, o dos Admins) se pisen en silencio.
  /// </summary>
  public byte[] RowVersion { get; set; } = Array.Empty<byte>();

  public virtual User User { get; set; } = null!;
}
