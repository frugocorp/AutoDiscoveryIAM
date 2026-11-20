namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

public interface ILoggerDelegates
{
  void LogInformation(string message, Exception? ex);
  void LogWarning(string message, Exception? ex);
  void LogError(string message, Exception? ex);
}
