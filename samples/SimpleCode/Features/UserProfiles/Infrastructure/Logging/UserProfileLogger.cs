using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using Microsoft.Extensions.Logging;

namespace TuProyecto.Features.UserProfiles.Infrastructure.Logging;

public class UserProfileLogger(ILogger<UserProfileLogger> logger) : LoggerBase<UserProfileLogger>(logger), IUserProfileLogger
{
  public void LogUserProfileCreated(long id, long userId)
  {
    LogInformation($"Perfil creado - Id: {id}, UserId: {userId}", null);
  }

  public void LogUserProfilesListed(int count)
  {
    LogInformation($"Se consultaron {count} perfiles.", null);
  }
}
