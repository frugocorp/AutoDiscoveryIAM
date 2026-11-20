namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

public class LoggerDelegates(ILogger logger) : ILoggerDelegates
{
  private readonly ILogger _logger = logger;
  // Hay que definir un catálogo para EventId, por el momento se usa 11001 a 11003
  private readonly Action<ILogger, string, Exception?> _logInformation =
      LoggerMessage.Define<string>(
          LogLevel.Information,
          new EventId(11001, "Information"),
          "{Message}");

  private static readonly Action<ILogger, string, Exception?> _logWarning =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(11002, "Warning"),
            "{Message}");
  private static readonly Action<ILogger, string, Exception?> _logError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(11003, "Error"),
            "{Message}");
  public void LogInformation(string message, Exception? ex)
  {
    _logInformation(_logger, message, ex);
  }

  public void LogWarning(string message, Exception? ex)
  {
    _logWarning(_logger, message, ex);
  }

  public void LogError(string message, Exception? ex)
  {
    _logError(_logger, message, ex);
  }
}
