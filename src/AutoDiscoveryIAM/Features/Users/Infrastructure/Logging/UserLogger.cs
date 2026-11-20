using System;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;

public class UserLogger(ILogger<UserLogger> logger) : LoggerBase<UserLogger>(logger), IUserLogger
{
  public void LogUserCreated(long userId, string username, string email, bool success = true)
  {
    if (success)
    {
      LogInformation($"Usuario creado - Id: {userId}, Username: {username}, Email: {email}", null);
    }
    else
    {
      LogError($"Usuario NO creado - Id: {userId}, Username: {username}, Email: {email}", null);
    }
  }

  public void LogUserUpdated(long userId, string username, bool success = true)
  {
    if (success)
    {
      LogInformation($"Usuario actualizado - Id: {userId}, Username: {username}", null);
    }
    else
    {
      LogError($"Usuario NO actualizado - Id: {userId}, Username: {username}", null);
    }
  }

  public void LogUserDeleted(long userId, string username, bool success = true)
  {
    if (success)
    {
      LogInformation($"Usuario eliminado - Id: {userId}, Username: {username}", null);
    }
    else
    {
      LogError($"Usuario NO eliminado - Id: {userId}, Username: {username}", null);
    }
  }

  public void LogUserRestored(long userId, string username, bool success = true)
  {
    if (success)
    {
      LogInformation($"Usuario restaurado - Id: {userId}, Username: {username}", null);
    }
    else
    {
      LogError($"Usuario NO restaurado - Id: {userId}, Username: {username}", null);
    }
  }

  public void LogUserNotFound(long userId)
  {
    LogWarning($"Usuario no encontrado - Id: {userId}", null);
  }

  public void LogUserNotFoundByPublicId(Guid publicId)
  {
    LogWarning($"Usuario no encontrado - PublicId: {publicId}", null);
  }

  public void LogUserUpdatedSuccessfully(Guid publicId, string details)
  {
    LogInformation($"Usuario actualizado exitosamente - PublicId: {publicId}, Detalles: {details}", null);
  }

  public void LogUsersRetrieved(int count)
  {
    LogInformation($"Usuarios recuperados - Total: {count}", null);
  }

  public void LogUserRetrieved(long userId)
  {
    LogInformation($"Usuario recuperado - Id: {userId}", null);
  }

  public void LogPasswordChanged(long userId, bool success = true)
  {
    if (success)
    {
      LogInformation($"Contraseña cambiada - Id: {userId}", null);
    }
    else
    {
      LogWarning($"Intento fallido de cambio de contraseña - Id: {userId}", null);
    }
  }

  public void LogPasswordResetRequested(string email, bool success = true)
  {
    if (success)
    {
      LogInformation($"Reseteo de contraseña solicitado - Email: {email}", null);
    }
    else
    {
      LogWarning($"Reseteo de contraseña no procesado (correo inexistente o inactivo) - Email: {email}", null);
    }
  }

  public void LogPasswordResetConfirmed(long userId, bool success = true)
  {
    if (success)
    {
      LogInformation($"Reseteo de contraseña confirmado - Id: {userId}", null);
    }
    else
    {
      LogWarning($"Intento fallido de confirmacion de reseteo de contraseña - Id: {userId}", null);
    }
  }

  public void LogEmailChangeRequested(long userId, string requestedEmail)
  {
    LogInformation($"Cambio de email solicitado - UserId: {userId}, Email solicitado: {requestedEmail}", null);
  }

  public void LogEmailChangeReviewed(long requestId, long reviewedBy, bool approved)
  {
    var accion = approved ? "aprobada para ejecucion" : "rechazada";
    LogInformation($"Solicitud de cambio de email {accion} - RequestId: {requestId}, RevisadaPor: {reviewedBy}", null);
  }

  public void LogEmailChangeExecuted(long requestId, long userId, long executedBy)
  {
    LogInformation($"Cambio de email ejecutado - RequestId: {requestId}, UserId: {userId}, EjecutadoPor: {executedBy}", null);
  }
}
