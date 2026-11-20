// -----------------------------------------------------------------------
// <copyright file="SuperAdminAccessRequiredException.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;

/// <summary>
/// Excepción lanzada cuando un endpoint requiere acceso de Super Admin y el usuario no lo tiene.
/// </summary>
public class SuperAdminAccessRequiredException : Exception
{
    public ErrorInfo ErrorInfo { get; }
    public string? Reason { get; }

    public SuperAdminAccessRequiredException(string message)
        : base(message)
    {
        ErrorInfo = ErrorInfo.SuperAdminAccessRequired;
    }

    public SuperAdminAccessRequiredException(string message, string reason)
        : base(message)
    {
        ErrorInfo = ErrorInfo.SuperAdminAccessRequired;
        Reason = reason;
    }

    public SuperAdminAccessRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorInfo = ErrorInfo.SuperAdminAccessRequired;
    }
}
