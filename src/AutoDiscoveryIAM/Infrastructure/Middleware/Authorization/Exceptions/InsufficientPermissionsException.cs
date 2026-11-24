// -----------------------------------------------------------------------
// <copyright file="InsufficientPermissionsException.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;

/// <summary>
/// Excepción lanzada cuando un usuario no tiene permisos suficientes para acceder a un recurso.
/// </summary>
public class InsufficientPermissionsException : Exception
{
    public ErrorInfo ErrorInfo { get; }
    public string? RequiredPermission { get; }
    public Guid? UserId { get; }

    public InsufficientPermissionsException(string message)
        : base(message)
    {
        ErrorInfo = ErrorInfo.InsufficientPermissions;
    }

    public InsufficientPermissionsException(string message, string requiredPermission, Guid userId)
        : base(message)
    {
        ErrorInfo = ErrorInfo.InsufficientPermissions;
        RequiredPermission = requiredPermission;
        UserId = userId;
    }

    public InsufficientPermissionsException(string message, ErrorInfo errorInfo)
        : base(message)
    {
        ErrorInfo = errorInfo;
    }

    public InsufficientPermissionsException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorInfo = ErrorInfo.Status403Forbidden;
    }
}
