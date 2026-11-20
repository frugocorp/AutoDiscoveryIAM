namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;

/// <summary>
/// Punto único de registro de eventos de seguridad (login fallido, lockout, reuso de refresh token,
/// permisos denegados, etc.), con campos estructurados consistentes independientemente de qué
/// feature lo dispare. Deja lista la categoría de log (<see cref="SecurityEventLogger"/>) para que
/// cualquier sink futuro (Seq, ELK, SIEM) filtre y alerte sobre estos eventos sin cambios de código.
/// </summary>
public interface ISecurityEventLogger
{
  /// <summary>
  /// Registra un evento de seguridad.
  /// </summary>
  /// <param name="eventType">Tipo de evento, del catálogo cerrado <see cref="SecurityEventType"/>.</param>
  /// <param name="userIdentifier">Identificador del usuario involucrado (PublicId o email), si se conoce.</param>
  /// <param name="ipAddress">Dirección IP real del cliente, si se conoce.</param>
  /// <param name="path">Ruta del endpoint involucrado, si aplica.</param>
  /// <param name="details">Detalle adicional legible del evento (no debe incluir secretos ni tokens completos).</param>
  void LogSecurityEvent(
    SecurityEventType eventType,
    string? userIdentifier = null,
    string? ipAddress = null,
    string? path = null,
    string? details = null);
}
