// -----------------------------------------------------------------------
// <copyright file="PermissionLogConfig.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;

/// <summary>
/// Configuración de logging específica para la feature Permissions.
/// </summary>
/// <param name="logPath">Ruta base donde se almacenarán los logs de permisos.</param>
/// <remarks>
/// Permite configurar el nivel de logging, filtros y rutas específicas para operaciones RBAC.
/// </remarks>
public class PermissionLogConfig(string logPath) : BaseLogConfig(logPath, LogLevel.Information), IFeatureLogConfig
{
    /// <inheritdoc />
    public string FeatureName => "Permissions";

    /// <inheritdoc />
    public string GetLogFilePath(string environment)
    {
        return base.GetLogFilePath(FeatureName, environment);
    }

    /// <inheritdoc />
    public bool ShouldLogMessage(string category, string message)
    {
        // Aquí podemos implementar lógica específica para filtrar mensajes de permisos
        // Por ejemplo, podríamos filtrar mensajes sensibles o de debugging excesivo
        return true;
    }
}
