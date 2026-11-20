// -----------------------------------------------------------------------
// <copyright file="ExternalLoginExchangeCodeRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;

public interface IExternalLoginExchangeCodeRepository
{
  /// <summary>
  /// Genera y persiste (hasheado) un código de un solo uso para la sesión ya creada, y devuelve
  /// el valor en claro -- ese valor en claro nunca se vuelve a poder obtener después de esta llamada.
  /// </summary>
  Task<string> CreateAsync(LoginResponseDto session, TimeSpan expiry, CancellationToken cancellationToken);

  /// <summary>
  /// Canjea un código: si es válido (existe, no vencido, no usado ya), lo marca como consumido y
  /// devuelve la sesión que representaba. Un código ya consumido o vencido devuelve <c>null</c> --
  /// de un solo uso, sin excepciones.
  /// </summary>
  Task<LoginResponseDto?> ConsumeAsync(string plaintextCode, CancellationToken cancellationToken);
}

public class ExternalLoginExchangeCodeRepository(AutoDiscoveryIAMDbContext context) : IExternalLoginExchangeCodeRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public async Task<string> CreateAsync(LoginResponseDto session, TimeSpan expiry, CancellationToken cancellationToken)
  {
    var plaintextCode = GenerateCode();

    var entity = new ExternalLoginExchangeCode
    {
      CodeHash = ExternalLoginExchangeCodeHasher.Hash(plaintextCode),
      SessionPayload = JsonSerializer.Serialize(session),
      ExpiresAt = TimeHelpers.GetCurrentMexicoTime().Add(expiry),
    };

    _context.ExternalLoginExchangeCodes.Add(entity);
    await _context.SaveChangesAsync(cancellationToken);

    return plaintextCode;
  }

  public async Task<LoginResponseDto?> ConsumeAsync(string plaintextCode, CancellationToken cancellationToken)
  {
    var codeHash = ExternalLoginExchangeCodeHasher.Hash(plaintextCode);

    var entity = await _context.ExternalLoginExchangeCodes
      .FirstOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken);

    if (entity == null || entity.IsConsumed || entity.IsExpired)
    {
      return null;
    }

    entity.IsConsumed = true;
    entity.ConsumedAt = TimeHelpers.GetCurrentMexicoTime();
    try
    {
      await _context.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      // Otro canje simultáneo del mismo código ganó (IsConsumed es token de concurrencia): este
      // pierde igual que si el código ya estuviera usado. Se suelta la entidad para que el
      // UnitOfWorkMiddleware no reintente guardarla al cerrar la petición.
      _context.Entry(entity).State = EntityState.Detached;
      return null;
    }

    return JsonSerializer.Deserialize<LoginResponseDto>(entity.SessionPayload);
  }

  /// <summary>
  /// Base64url (sin padding) de 32 bytes aleatorios -- mismo generador (<see cref="RandomNumberGenerator"/>)
  /// que <see cref="RefreshToken.RefreshTokenService.GenerateTokenString"/>, pero URL-safe de
  /// entrada (sin '+'/'/'), ya que este código viaja en un fragmento de URL.
  /// </summary>
  private static string GenerateCode()
  {
    using var rng = RandomNumberGenerator.Create();
    var bytes = new byte[32];
    rng.GetBytes(bytes);
    return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }
}
