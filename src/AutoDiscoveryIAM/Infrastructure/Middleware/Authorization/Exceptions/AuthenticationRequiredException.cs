// -----------------------------------------------------------------------
// <copyright file="AuthenticationRequiredException.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;

/// <summary>
/// Excepción lanzada cuando un request requiere autenticación pero no se proporciona.
/// </summary>
public class AuthenticationRequiredException : Exception
{
  public ErrorInfo ErrorInfo { get; }

  public AuthenticationRequiredException(string message)
      : base(message)
  {
    ErrorInfo = ErrorInfo.AuthenticationRequired;
  }

  public AuthenticationRequiredException(string message, Exception innerException)
      : base(message, innerException)
  {
    ErrorInfo = ErrorInfo.AuthenticationRequired;
  }
}
