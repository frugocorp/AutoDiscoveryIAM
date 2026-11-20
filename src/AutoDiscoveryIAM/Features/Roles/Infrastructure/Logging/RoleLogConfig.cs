// -----------------------------------------------------------------------
// <copyright file="RoleLogConfig.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;

public class RoleLogConfig(string logPath) : BaseLogConfig(logPath, LogLevel.Information), IFeatureLogConfig
{
  public string FeatureName => "Roles";

  public string GetLogFilePath(string environment)
  {
    return base.GetLogFilePath(FeatureName, environment);
  }

  public bool ShouldLogMessage(string category, string message)
  {
    // Aquí podemos implementar lógica específica para filtrar mensajes
    return true;
  }
}
