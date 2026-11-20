using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Services;

public class LoggerService(ILogger<LoggerService> logger) : LoggerBase<LoggerService>(logger), ILoggerBase<LoggerService>
{
}
