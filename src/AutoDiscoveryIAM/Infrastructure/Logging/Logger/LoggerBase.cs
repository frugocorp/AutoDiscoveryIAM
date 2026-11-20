using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

public class LoggerBase<T>(ILogger<T> logger) : ILoggerBase<T>
{
  private readonly LoggerDelegates _loggerDelegates = new(logger);
  // Hay que definir un catálogo para EventId, por el momento se usa 11001 a 11003
  public void LogInformation(string message, Exception? ex)
  {
    _loggerDelegates.LogInformation(LogSanitizer.Clean(message), ex);
  }

  public void LogWarning(string message, Exception? ex)
  {
    _loggerDelegates.LogWarning(LogSanitizer.Clean(message), ex);
  }

  public void LogError(string message, Exception? ex)
  {
    _loggerDelegates.LogError(LogSanitizer.Clean(message), ex);
  }
}
