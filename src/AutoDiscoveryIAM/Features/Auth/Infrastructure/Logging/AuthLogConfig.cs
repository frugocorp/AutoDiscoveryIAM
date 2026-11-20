// -----------------------------------------------------------------------
// <copyright file="AuthLogConfig.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;

public class AuthLogConfig(string logPath) : BaseLogConfig(logPath, LogLevel.Information), IFeatureLogConfig
{
  public string FeatureName => "Auth";

  public string GetLogFilePath(string environment)
  {
    return base.GetLogFilePath(FeatureName, environment);
  }

  public bool ShouldLogMessage(string category, string message)
  {
    // Aquí puedes implementar lógica específica para filtrar mensajes
    return true;
  }
}
