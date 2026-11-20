using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

namespace TuProyecto.Features.UserProfiles.Infrastructure.Logging;

/// <summary>
/// Logger propio de la feature UserProfiles -- mismo patrón que usa la librería internamente
/// (un I{Entidad}Logger por feature, envolviendo ILoggerBase&lt;T&gt;), en vez de llamar
/// ILogger&lt;T&gt; directo en cada Handler.
/// </summary>
public interface IUserProfileLogger : ILoggerBase<IUserProfileLogger>
{
  void LogUserProfileCreated(long id, long userId);
  void LogUserProfilesListed(int count);
}
