using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;

/// <inheritdoc cref="ISecurityEventLogger"/>
public sealed partial class SecurityEventLogger(ILogger<SecurityEventLogger> logger) : ISecurityEventLogger
{
  private readonly ILogger<SecurityEventLogger> _logger = logger;

  public void LogSecurityEvent(
    SecurityEventType eventType,
    string? userIdentifier = null,
    string? ipAddress = null,
    string? path = null,
    string? details = null)
  {
    LogSecurityEventCore(
      _logger,
      eventType,
      LogSanitizer.Clean(userIdentifier ?? "unknown"),
      LogSanitizer.Clean(ipAddress ?? "unknown"),
      LogSanitizer.Clean(path ?? "unknown"),
      LogSanitizer.Clean(details ?? string.Empty));
  }

  [LoggerMessage(
    EventId = 12001,
    Level = LogLevel.Warning,
    Message = "SecurityEvent {EventType} user={UserIdentifier} ip={IpAddress} path={Path} details={Details}")]
  private static partial void LogSecurityEventCore(
    ILogger logger,
    SecurityEventType eventType,
    string userIdentifier,
    string ipAddress,
    string path,
    string details);
}
