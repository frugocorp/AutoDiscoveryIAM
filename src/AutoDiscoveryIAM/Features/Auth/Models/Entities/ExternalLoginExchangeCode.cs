// -----------------------------------------------------------------------
// <copyright file="ExternalLoginExchangeCode.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;

/// <summary>
/// Código de un solo uso y vida cortísima que representa una sesión ya creada (login social
/// exitoso), pendiente de canjear por el JWT/refresh token reales. Mismo patrón que usan
/// Auth0/Okta/Cognito para entregar una sesión a una SPA vía redirect sin exponer el token
/// directamente en la URL: el redirect solo lleva este código de un solo uso (<c>#code=...</c>);
/// el token real se obtiene con un <c>POST</c> aparte a <c>/auth/external-login/exchange</c>, que
/// nunca queda en el historial del navegador ni en logs de servidor.
/// </summary>
public class ExternalLoginExchangeCode : AuditableEntity
{
  public long Id { get; set; }

  /// <summary>
  /// Hash (SHA-256) del código. El valor en claro nunca se persiste, solo se devuelve una vez, en
  /// el redirect -- igual que <see cref="Commands.RefreshToken.RefreshTokenHasher"/> para refresh
  /// tokens.
  /// </summary>
  public string CodeHash { get; set; } = string.Empty;

  /// <summary>
  /// <see cref="Commands.Login.LoginResponseDto"/> ya generado (Token/ExpiresAt/RefreshToken),
  /// serializado -- se devuelve tal cual al canjear el código, sin regenerar nada.
  /// </summary>
  public string SessionPayload { get; set; } = string.Empty;

  public DateTime ExpiresAt { get; set; }

  public bool IsConsumed { get; set; } = false;

  public DateTime? ConsumedAt { get; set; }

  public bool IsExpired => TimeHelpers.GetCurrentMexicoTime() >= ExpiresAt;
}
