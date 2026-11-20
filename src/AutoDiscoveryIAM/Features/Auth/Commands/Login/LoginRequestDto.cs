// -----------------------------------------------------------------------
// <copyright file="LoginRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

/// <summary>
/// DTO para los datos requeridos en el Login.
/// </summary>
/// <param name="Email">
/// Email del usuario. Validado por <see cref="LoginCommandValidator"/> y las reglas de <see cref="AuthValidationRules"/>.
/// </param>
/// <param name="Password">
/// Contraseña del usuario. Validado por <see cref="LoginCommandValidator"/> y las reglas de <see cref="AuthValidationRules"/>.
/// </param>
/// <remarks>
/// Este DTO es utilizado por <see cref="LoginCommand"/> para encapsular los datos de autenticación.
///
/// CREDENCIALES DE EJEMPLO:
///
/// PRODUCCIÓN:
/// - Email: admin@frugo.com
/// - Password: FrugoAdmin2025!
///
/// DESARROLLO:
/// - Email: admin@example.com
/// - Password: AdminPass123!
/// - Email: user@example.com
/// - Password: UserPass123!
/// </remarks>
public record LoginRequestDto(
    [EmailAddress]
    string Email,

    string Password)
{
  /// <summary>
  /// Convierte el DTO a Command incluyendo información de contexto HTTP.
  /// </summary>
  public LoginCommand ToCommand(HttpContext context)
  {
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    var userAgent = context.Request.Headers.UserAgent.ToString();

    return new LoginCommand(Email, Password, ipAddress, userAgent);
  }
};
