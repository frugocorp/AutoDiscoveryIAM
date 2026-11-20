namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

public interface ILoggerBase<T>
{
  void LogInformation(string message, Exception? ex);
  void LogWarning(string message, Exception? ex);
  void LogError(string message, Exception? ex);
}
