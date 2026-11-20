// -----------------------------------------------------------------------
// <copyright file="DevelopmentAccessRequiredException.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;

/// <summary>
/// Excepción lanzada cuando un endpoint requiere acceso de Development y el usuario no lo tiene.
/// </summary>
public class DevelopmentAccessRequiredException : Exception
{
    public ErrorInfo ErrorInfo { get; }
    public string? Reason { get; }

    public DevelopmentAccessRequiredException(string message)
        : base(message)
    {
        ErrorInfo = ErrorInfo.DevelopmentAccessRequired;
    }

    public DevelopmentAccessRequiredException(string message, string reason)
        : base(message)
    {
        ErrorInfo = ErrorInfo.DevelopmentAccessRequired;
        Reason = reason;
    }

    public DevelopmentAccessRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorInfo = ErrorInfo.DevelopmentAccessRequired;
    }
}
