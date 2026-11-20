namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Expone datos del usuario autenticado para auditoria sin depender del DbContext.
/// </summary>
public interface ICurrentAuditUserService
{
    CurrentAuditUser GetCurrentUser();
}
