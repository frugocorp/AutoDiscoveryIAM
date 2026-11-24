// -----------------------------------------------------------------------
// <copyright file="RefreshTokenRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Repositorio para operaciones con RefreshToken.
/// </summary>
public interface IRefreshTokenRepository
{
  Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
  Task<RefreshTokenEntity?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
  Task ReloadAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default);
  Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default);
  Task UpdateAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default);
  Task RevokeUserTokensAsync(long userId, string reason, CancellationToken cancellationToken = default);
  Task CleanExpiredTokensAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación del repositorio para RefreshToken.
/// </summary>
public class RefreshTokenRepository(AutoDiscoveryIAMDbContext dbContext) : IRefreshTokenRepository
{
  private readonly AutoDiscoveryIAMDbContext _dbContext = dbContext;

  /// <summary>
  /// Obtiene un refresh token por su valor, incluyendo el usuario.
  /// </summary>
  public async Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
  {
    var hash = RefreshTokenHasher.Hash(token);

    return await _dbContext.RefreshTokens
      .Include(rt => rt.User)
      .FirstOrDefaultAsync(rt => rt.Token == hash, cancellationToken);
  }

  /// <summary>
  /// Obtiene un refresh token por el hash ya calculado (el que guardan las columnas Token y
  /// ReplacedByToken).
  /// </summary>
  public Task<RefreshTokenEntity?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
  {
    return _dbContext.RefreshTokens
      .AsNoTracking()
      .FirstOrDefaultAsync(rt => rt.Token == tokenHash, cancellationToken);
  }

  /// <summary>
  /// Descarta los cambios en memoria del token y vuelve a leerlo de la base (tras perder una carrera
  /// de concurrencia).
  /// </summary>
  public Task ReloadAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default)
  {
    return _dbContext.Entry(refreshToken).ReloadAsync(cancellationToken);
  }

  /// <summary>
  /// Crea un nuevo refresh token.
  /// </summary>
  public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default)
  {
    _dbContext.RefreshTokens.Add(refreshToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
    return refreshToken;
  }

  /// <summary>
  /// Actualiza un refresh token existente.
  /// </summary>
  public async Task UpdateAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default)
  {
    _dbContext.RefreshTokens.Update(refreshToken);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>
  /// Revoca todos los tokens activos de un usuario.
  /// </summary>
  public async Task RevokeUserTokensAsync(long userId, string reason, CancellationToken cancellationToken = default)
  {
    var activeTokens = await _dbContext.RefreshTokens
      .Where(rt => rt.UserId == userId && !rt.IsRevoked)
      .ToListAsync(cancellationToken);

    foreach (var token in activeTokens)
    {
      token.IsRevoked = true;
      token.RevokedAt = TimeHelpers.GetCurrentMexicoTime();
      token.RevocationReason = reason;
    }

    await _dbContext.SaveChangesAsync(cancellationToken);
  }

  /// <summary>
  /// Limpia tokens expirados de la base de datos.
  /// </summary>
  public async Task CleanExpiredTokensAsync(CancellationToken cancellationToken = default)
  {
    var currentTime = TimeHelpers.GetCurrentMexicoTime();
    var expiredTokens = await _dbContext.RefreshTokens
      .Where(rt => rt.ExpiresAt < currentTime)
      .ToListAsync(cancellationToken);

    _dbContext.RefreshTokens.RemoveRange(expiredTokens);
    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
