using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;

public class UserLogConfig(string logPath)
  : BaseLogConfig(logPath, LogLevel.Information), IFeatureLogConfig
{
  public string FeatureName => "Users";

  public string GetLogFilePath(string environment)
    => base.GetLogFilePath(FeatureName, environment);

  public bool ShouldLogMessage(string category, string message)
  {
    // Aquí podemos implementar lógica específica para filtrar mensajes
    return true;
  }
}

