namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;

/// <summary>
/// Catálogo cerrado de eventos de seguridad que se registran de forma centralizada,
/// filtrable por tipo independientemente de la feature que lo dispare.
/// </summary>
public enum SecurityEventType
{
  LoginFailed,
  AccountLockedOut,
  TwoFactorFailed,
  RefreshTokenReuseDetected,
  PermissionDenied,
  AccessTokenRejectedBlacklisted,
}
