// -----------------------------------------------------------------------
// <copyright file="IFeatureLogConfig.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;

public interface IFeatureLogConfig
{
  string FeatureName { get; }
  string GetLogFilePath(string environment);
  bool ShouldLogMessage(string category, string message);
}
