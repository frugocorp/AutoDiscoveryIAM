// -----------------------------------------------------------------------
// <copyright file="AuthorizationInfo.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// Clase auxiliar para contener información de autorización extraída de metadatos de endpoints.
/// </summary>
internal sealed class AuthorizationInfo
{
  public bool HasPermissionsNotRequired { get; set; }
  public string? PermissionsNotRequiredReason { get; set; }
  public bool RequiresDevelopmentAccess { get; set; }
  public string? RequiresDevelopmentAccessReason { get; set; }
  public bool RequiresSuAdminAccess { get; set; }
  public string? RequiresSuAdminAccessReason { get; set; }
  public bool HasRequirePermissions { get; set; }
  public string? RequirePermissionsNote { get; set; }
}