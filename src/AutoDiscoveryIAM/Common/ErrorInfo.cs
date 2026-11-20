using System.Text.Json.Serialization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

/// <summary>
/// Representa información estructurada sobre un error estándar de operación.
/// Puede usarse en Handlers, Repositorios, Servicios, etc.
/// </summary>
/// <remarks>
/// Constructor privado para evitar instanciación externa directa.
/// Usa las propiedades estáticas predefinidas.
/// </remarks>
[method: JsonConstructor]
public class ErrorInfo(int status, int statusCode, string key, string description)
{
  /// <summary>
  /// Código de estatus interno o de negocio (más detallado que el HTTP).
  /// </summary>
  public int Status { get; } = status;

  /// <summary>
  /// Código de estado HTTP asociado al error.
  /// </summary>
  public int StatusCode { get; } = statusCode;

  /// <summary>
  /// Clave única del error (útil para lógica, internacionalización o identificación).
  /// </summary>
  public string Key { get; } = key;

  /// <summary>
  /// Descripción humana del error.
  /// </summary>
  public string Description { get; } = description;

  // Si necesitas crear errores personalizados en tiempo de ejecución:
  public static ErrorInfo Custom(int status, int statusCode, string key, string description)
      => new(status, statusCode, key, description);

  /// <summary>
  /// Fuente única de verdad para el ejemplo de Swagger de este error — usado por
  /// <c>EndpointOperationDocs</c> para documentar cualquier respuesta de error sin necesitar
  /// una clase <c>IExamplesProvider</c> dedicada por código de estado.
  /// </summary>
  public ApiResponse<object> ToExampleResponse() => new(false, null, Description, this);

  // Propiedades definidas
  public static ErrorInfo Status400Validation { get; } = new(StatusCodes.Status400BadRequest, StatusCodes.Status400BadRequest, "Status400Validation", "Error de validación de datos (formato, requeridos, etc.)");
  public static ErrorInfo Status401Unauthorized { get; } = new(StatusCodes.Status401Unauthorized, StatusCodes.Status401Unauthorized, "Status401Unauthorized", "El usuario no está autenticado");
  public static ErrorInfo Status403Forbidden { get; } = new(StatusCodes.Status403Forbidden, StatusCodes.Status403Forbidden, "Status403Forbidden", "El usuario está autenticado pero no tiene permisos");
  public static ErrorInfo Status404NotFound { get; } = new(StatusCodes.Status404NotFound, StatusCodes.Status404NotFound, "Status404NotFound", "El recurso no existe");
  public static ErrorInfo Status408Timeout { get; } = new(StatusCodes.Status408RequestTimeout, StatusCodes.Status408RequestTimeout, "Status408Timeout", "La operación excedió el tiempo permitido");
  public static ErrorInfo Status409Conflict { get; } = new(StatusCodes.Status409Conflict, StatusCodes.Status409Conflict, "Status409Conflict", "Conflicto de estado (por ejemplo, no se permiten duplicados y el recurso ya existe, o estado inconsistente)");
  public static ErrorInfo Status429TooManyRequests { get; } = new(StatusCodes.Status429TooManyRequests, StatusCodes.Status429TooManyRequests, "Status429TooManyRequests", "Se excedió el límite de peticiones permitido; reintentar después del tiempo indicado en Retry-After");

  // Para códigos que NO existen en StatusCodes, usa el valor numérico real y el StatusCode "relacionado" más cercano
  public static ErrorInfo Status422Business { get; } = new(422, StatusCodes.Status409Conflict, "Status422Business", "Violación de una regla de negocio");
  public static ErrorInfo Status424Dependency { get; } = new(424, StatusCodes.Status503ServiceUnavailable, "Status424Dependency", "Error en un servicio externo o dependencia");

  public static ErrorInfo Status500Unknown { get; } = new(StatusCodes.Status500InternalServerError, StatusCodes.Status500InternalServerError, "Status500Unknown", "Error inesperado/no clasificado");
  public static ErrorInfo Status503Unavailable { get; } = new(StatusCodes.Status503ServiceUnavailable, StatusCodes.Status503ServiceUnavailable, "Status503Unavailable", "Servicio temporalmente no disponible");
  public static ErrorInfo Status999Example { get; } = new(999, StatusCodes.Status500InternalServerError, "Status999Example", "Descripción del StatusCode");

  // Authorization-specific error codes
  public static ErrorInfo AuthenticationRequired { get; } = new(StatusCodes.Status401Unauthorized, StatusCodes.Status401Unauthorized, "AUTH_AUTHENTICATION_REQUIRED", "Se requiere autenticación para acceder a este recurso");
  public static ErrorInfo AccountPendingOrInactive { get; } = new(StatusCodes.Status401Unauthorized, StatusCodes.Status401Unauthorized, "AUTH_ACCOUNT_PENDING_OR_INACTIVE", "La cuenta esta pendiente de activacion o inactiva");
  public static ErrorInfo AccountLockedOut { get; } = new(StatusCodes.Status401Unauthorized, StatusCodes.Status401Unauthorized, "AUTH_ACCOUNT_LOCKED_OUT", "La cuenta esta bloqueada temporalmente por multiples intentos fallidos");
  public static ErrorInfo InsufficientPermissions { get; } = new(StatusCodes.Status403Forbidden, StatusCodes.Status403Forbidden, "AUTH_INSUFFICIENT_PERMISSIONS", "Permisos insuficientes para acceder a este recurso");
  public static ErrorInfo DevelopmentAccessRequired { get; } = new(StatusCodes.Status403Forbidden, StatusCodes.Status403Forbidden, "AUTH_DEVELOPMENT_ACCESS_REQUIRED", "Se requiere acceso de desarrollo para este recurso");
  public static ErrorInfo SuperAdminAccessRequired { get; } = new(StatusCodes.Status403Forbidden, StatusCodes.Status403Forbidden, "AUTH_SUPER_ADMIN_REQUIRED", "Se requiere acceso de SuperAdmin para este recurso");

}
