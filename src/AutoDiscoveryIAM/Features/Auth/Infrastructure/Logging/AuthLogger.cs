// -----------------------------------------------------------------------
// <copyright file="AuthLogger.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;

public class AuthLogger(ILogger<AuthLogger> logger) : LoggerBase<AuthLogger>(logger), IAuthLogger
{
  public void LogLoginSuccess(string username)
  {
    LogInformation($"Login exitoso - Usuario: {username}", null);
  }

  public void LogLoginFailed(string username)
  {
    LogWarning($"Intento de login fallido - Usuario: {username}", null);
  }

  public void LogUserBlocked(string username)
  {
    LogWarning($"Usuario bloqueado - Usuario: {username}", null);
  }

  public void LogTokenGenerated(string username)
  {
    LogInformation($"Token generado - Usuario: {username}", null);
  }

  public void LogTokenInvalid(string username)
  {
    LogWarning($"Token inválido - Usuario: {username}", null);
  }

  public void LogLogout(string username)
  {
    LogInformation($"Logout exitoso - Usuario: {username}", null);
  }

  public void LogUnauthorizedAccess(string username, string resource)
  {
    LogWarning($"Acceso no autorizado - Usuario: {username}, Recurso: {resource}", null);
  }

  public void LogUsersRetrieved(int count)
  {
    LogInformation($"Usuarios recuperados - Total: {count}", null);
  }

  public void LogUserRetrieved(string username)
  {
    LogInformation($"Usuario recuperado - Usuario: {username}", null);
  }

  public void LogUserNotFound(string username)
  {
    LogWarning($"Usuario no encontrado - Usuario: {username}", null);
  }
}
