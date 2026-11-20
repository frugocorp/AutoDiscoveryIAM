// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Docs;

/// <summary>
/// Datos de documentación de <c>SyncPermissionsEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class SyncPermissionsOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription =>
    "Sincronizacion ejecutada correctamente. La respuesta incluye estadisticas de permisos descubiertos, creados, actualizados y marcados obsoletos.";

  protected override object SuccessExample { get; } = new SyncPermissionsCommandResponse(
    success: true,
    data: new SyncPermissionsCommandResponseDto(
      OrchestrationResult: new AnalyzeUpdatedPermissionsResponseDto(
        PermissionsDiscovered: 15,
        PermissionsCreated: 3,
        PermissionsUpdated: 2,
        PermissionsObsoleted: 1,
        PermissionsRecovered: 0,
        TotalExecutionTimeMs: 1250,
        DiscoveryExecutionTimeMs: 300,
        SyncExecutionTimeMs: 950,
        FilesScanned: 45,
        FeaturesAnalyzed: new List<string> { "users", "roles", "permissions", "pedidos" },
        DryRunMode: true,
        EndpointsWithoutPermission: new List<string> { "GET /legacy/reporte (MiApi.Legacy)" })));

  protected override object RequestExample { get; } = new SyncPermissionsRequestDto(
    ForceFullSync: false,
    DryRun: true);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.DevelopmentAccessRequired.StatusCode] = ErrorInfo.DevelopmentAccessRequired,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status500Unknown.StatusCode] = ErrorInfo.Status500Unknown,
  };
}
