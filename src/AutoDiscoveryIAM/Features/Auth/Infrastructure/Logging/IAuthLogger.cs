// -----------------------------------------------------------------------
// <copyright file="IPostLogger.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging
{
  /// <summary>
  /// Interfaz para el logger especializado en operaciones sobre entidades Post.
  /// </summary>
  public interface IAuthLogger : ILoggerBase<IAuthLogger>
  {
    void LogLoginSuccess(string username);
    void LogLoginFailed(string username);
    void LogUserBlocked(string username);
    void LogTokenGenerated(string username);
    void LogTokenInvalid(string username);
    void LogLogout(string username);
    void LogUnauthorizedAccess(string username, string resource);
    void LogUsersRetrieved(int count);
    void LogUserRetrieved(string username);
    void LogUserNotFound(string username);
  }
}
