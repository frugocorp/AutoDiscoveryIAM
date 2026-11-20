using System.Security.Claims;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Lee claims del usuario autenticado para resolver el Id interno usado en auditoria.
/// </summary>
public class CurrentAuditUserService : ICurrentAuditUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentAuditUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentAuditUser GetCurrentUser()
    {
        var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated)
        {
            return new CurrentAuditUser(null, null, null, null, Array.Empty<string>());
        }

        var id = TryResolveUserId(identity);
        var publicId = TryResolvePublicId(identity);
        var email = identity.FindFirst(ClaimTypes.Email)?.Value ?? identity.FindFirst("email")?.Value;
        var fullName = identity.FindFirst(ClaimTypes.Name)?.Value;
        var roles = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();

        return new CurrentAuditUser(id, publicId, email, fullName, roles);
    }

    private static long? TryResolveUserId(ClaimsIdentity identity)
    {
        var raw = identity.FindFirst("UserId")?.Value
          ?? identity.FindFirst(ClaimTypes.Sid)?.Value;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return long.TryParse(raw, out var id) ? id : null;
    }

    private static Guid? TryResolvePublicId(ClaimsIdentity identity)
    {
        var raw = identity.FindFirst("PublicId")?.Value
          ?? identity.FindFirst("sub")?.Value
          ?? identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Guid.TryParse(raw, out var publicId) ? publicId : null;
    }
}
