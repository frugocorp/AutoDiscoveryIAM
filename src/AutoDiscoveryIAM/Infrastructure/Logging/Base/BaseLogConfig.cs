// -----------------------------------------------------------------------
// <copyright file="BaseLogConfig.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;

public abstract class BaseLogConfig(string logPath, LogLevel minimumLevel = LogLevel.Information, bool includeScopes = true)
{
  public string LogPath { get; } = logPath;

  public LogLevel MinimumLevel { get; } = minimumLevel;

  public bool IncludeScopes { get; } = includeScopes;

  public virtual string GetLogFilePath(string featureName, string environment)
  {
    return Path.Combine(LogPath, $"{featureName}.{environment}.log");
  }
}
