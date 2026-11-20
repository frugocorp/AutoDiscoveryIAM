// -----------------------------------------------------------------------
// <copyright file="ExternalLoginSessionService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;

public class ExternalLoginSessionService(
  IJwtClaimsFactory jwtClaimsFactory,
  IRefreshTokenService refreshTokenService,
  IRefreshTokenRepository refreshTokenRepository)
{
    private readonly IJwtClaimsFactory _jwtClaimsFactory = jwtClaimsFactory;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;

    public async Task<LoginResponseDto> CreateSessionAsync(User user, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var (tokenString, expiresAt) = _jwtClaimsFactory.BuildAccessToken(user);

        var refreshTokenEntity = _refreshTokenService.GenerateRefreshToken(user, ipAddress, userAgent);
        await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

        return new LoginResponseDto(tokenString, expiresAt, refreshTokenEntity.PlaintextToken!);
    }
}
