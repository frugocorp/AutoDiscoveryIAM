using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;

/// <summary>
/// Implementación del servicio de contexto de autenticación.
/// Extrae información del JWT sin dependencias de otros dominios.
/// </summary>
public class AuthContextService : IAuthContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AutoDiscoveryIAMDbContext _context;

    public AuthContextService(IHttpContextAccessor httpContextAccessor, AutoDiscoveryIAMDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetCurrentUserAsync()
    {
        var userPublicId = GetCurrentUserPublicId();
        if (userPublicId == null)
        {
            return null;
        }

        return await _context.Users
            .FirstOrDefaultAsync(u => u.PublicId == userPublicId.Value);
    }

    /// <inheritdoc />
    public Guid? GetCurrentUserPublicId()
    {
        var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

        if (identity == null || !identity.IsAuthenticated)
        {
            return null;
        }

        var publicIdClaim = identity.FindFirst("PublicId") ?? identity.FindFirst("sub") ?? identity.FindFirst(ClaimTypes.NameIdentifier);

        if (publicIdClaim == null || string.IsNullOrWhiteSpace(publicIdClaim.Value))
        {
            return null;
        }

        return Guid.TryParse(publicIdClaim.Value, out var publicId) ? publicId : null;
    }

    /// <inheritdoc />
    public string? GetCurrentUserEmail()
    {
        var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

        if (identity == null || !identity.IsAuthenticated)
        {
            return null;
        }

        var emailClaim = identity.FindFirst(ClaimTypes.Email) ?? identity.FindFirst("email");

        return emailClaim?.Value;
    }

    /// <inheritdoc />
    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }

    /// <inheritdoc />
    public IEnumerable<Claim> GetCurrentUserClaims()
    {
        var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

        if (identity == null || !identity.IsAuthenticated)
        {
            return Enumerable.Empty<Claim>();
        }

        return identity.Claims;
    }
}
