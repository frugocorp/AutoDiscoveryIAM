using System.Security.Claims;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Representa la identidad auditada del usuario actual obtenida desde claims.
/// </summary>
public sealed record CurrentAuditUser(
  long? Id,
  Guid? PublicId,
  string? Email,
  string? FullName,
  IReadOnlyCollection<string> Roles)
{
    public bool IsAuthenticated => Id.HasValue;
}
