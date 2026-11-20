// -----------------------------------------------------------------------
// <copyright file="IRoleLogger.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Base;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;

/// <summary>
/// Interface for logging messages for the Roles feature.
/// </summary>
public interface IRoleLogger : ILoggerBase<IRoleLogger>
{
  void LogRolesListed(int count);
  void LogRoleRetrieved(long id);
  void LogRoleNotFound(long id);
  void LogRoleRetrievedByPublicId(Guid publicId);
  void LogRoleNotFoundByPublicId(Guid publicId);
  void LogRoleCreated(long id, string name, bool success = true);
  void LogRoleUpdated(long id, string name, bool success = true);
  void LogRoleDeleted(long id, bool success = true);
  void LogRestoreRoleAttempt(Guid publicId);
  void LogRoleRestored(Guid publicId, string name);
  void LogRestoreRoleNameConflict(Guid publicId, string name);
  void LogRestoreRoleError(Guid publicId, Exception ex);
}
