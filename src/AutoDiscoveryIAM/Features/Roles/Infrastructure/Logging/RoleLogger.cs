// -----------------------------------------------------------------------
// <copyright file="RoleLogger.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;

/// <summary>
/// Logger especializado para operaciones sobre entidades Role.
/// </summary>
public class RoleLogger(ILogger<RoleLogger> logger) : LoggerBase<RoleLogger>(logger), IRoleLogger
{
  public void LogRolesListed(int count)
  {
    LogInformation($"Se consultaron {count} roles.", null);
  }

  public void LogRoleRetrieved(long id)
  {
    LogInformation($"Se consultó el rol con ID {id}.", null);
  }

  public void LogRoleCreated(long id, string name, bool success = true)
  {
    if (success)
    {
      LogInformation($"Rol creado - ID: {id}, Nombre: {name}", null);
    }
    else
    {
      LogError($"Rol NO creado - ID: {id}, Nombre: {name}", null);
    }
  }

  public void LogRoleUpdated(long id, string name, bool success = true)
  {
    if (success)
    {
      LogInformation($"Rol actualizado - ID: {id}, Nombre: {name}", null);
    }
    else
    {
      LogError($"Rol NO actualizado - ID: {id}, Nombre: {name}", null);
    }
  }

  public void LogRoleDeleted(long id, bool success = true)
  {
    if (success)
    {
      LogInformation($"Rol eliminado - ID: {id}", null);
    }
    else
    {
      LogError($"Rol NO eliminado - ID: {id}", null);
    }
  }

  public void LogRoleNotFound(long id)
  {
    LogWarning($"Rol no encontrado - ID: {id}", null);
  }

  public void LogRoleRetrievedByPublicId(Guid publicId)
  {
    LogInformation($"Se consultó el rol con PublicId {publicId}.", null);
  }

  public void LogRoleNotFoundByPublicId(Guid publicId)
  {
    LogWarning($"Rol no encontrado - PublicId: {publicId}", null);
  }

  public void LogRestoreRoleAttempt(Guid publicId)
  {
    LogInformation($"Intentando restaurar rol - PublicId: {publicId}", null);
  }

  public void LogRoleRestored(Guid publicId, string name)
  {
    LogInformation($"Rol restaurado exitosamente - PublicId: {publicId}, Nombre: {name}", null);
  }

  public void LogRestoreRoleNameConflict(Guid publicId, string name)
  {
    LogWarning($"Conflicto de nombre al restaurar rol - PublicId: {publicId}, Nombre: {name}", null);
  }

  public void LogRestoreRoleError(Guid publicId, Exception ex)
  {
    LogError($"Error al restaurar rol - PublicId: {publicId}", ex);
  }
}
