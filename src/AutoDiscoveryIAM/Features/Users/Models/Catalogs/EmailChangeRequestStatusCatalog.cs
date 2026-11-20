// -----------------------------------------------------------------------
// <copyright file="EmailChangeRequestStatusCatalog.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

/// <summary>
/// Estados del flujo de cambio de email: el usuario solicita, un Admin revisa
/// (aprueba para ejecución o rechaza), y solo un SuAdmin ejecuta el cambio real.
/// </summary>
public static class EmailChangeRequestStatusCatalog
{
  public const string Pending = "pending";
  public const string ApprovedForExecution = "approved_for_execution";
  public const string Rejected = "rejected";
  public const string Executed = "executed";
}
