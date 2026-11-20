// -----------------------------------------------------------------------
// <copyright file="AuthorizationMiddlewareException.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Exceptions;

/// <summary>
/// Excepción unificada para errores de autorización en el middleware.
/// Proporciona información detallada sobre el tipo de validación de autorización
/// que falló y permite respuestas HTTP apropiadas.
/// </summary>
/// <remarks>
/// Esta excepción reemplaza el patrón anterior de múltiples excepciones específicas
/// para cada tipo de autorización, proporcionando un manejo unificado y consistente
/// de todos los escenarios de fallo de autorización en el middleware.
///
/// Incluye información contextual rica como el tipo de validación, endpoint afectado,
/// y razones específicas del fallo para facilitar debugging y logging.
/// </remarks>
public sealed class AuthorizationMiddlewareException : Exception
{
    /// <summary>
    /// Tipo de validación de autorización que causó la excepción.
    /// </summary>
    public AuthorizationValidationTypes ValidationType { get; }

    /// <summary>
    /// Ruta del endpoint que causó el fallo de autorización.
    /// </summary>
    public string EndpointPath { get; }

    /// <summary>
    /// Método HTTP del endpoint que causó el fallo.
    /// </summary>
    public string HttpMethod { get; }

    /// <summary>
    /// Identificador del usuario que intentó acceder (si está disponible).
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Código de estado HTTP apropiado para este tipo de fallo.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Información adicional específica del contexto del fallo.
    /// </summary>
    public IDictionary<string, object> ContextData { get; }

    /// <summary>
    /// Constructor principal para crear una excepción de autorización.
    /// </summary>
    /// <param name="validationType">Tipo de validación que falló.</param>
    /// <param name="endpointPath">Ruta del endpoint.</param>
    /// <param name="httpMethod">Método HTTP.</param>
    /// <param name="message">Mensaje descriptivo del error.</param>
    /// <param name="userId">ID del usuario (opcional).</param>
    /// <param name="innerException">Excepción interna (opcional).</param>
    private AuthorizationMiddlewareException(
        AuthorizationValidationTypes validationType,
        string endpointPath,
        string httpMethod,
        string message,
        string? userId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ValidationType = validationType ?? throw new ArgumentNullException(nameof(validationType));
        EndpointPath = endpointPath ?? throw new ArgumentNullException(nameof(endpointPath));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        UserId = userId;
        StatusCode = DetermineStatusCode(validationType);
        ContextData = new Dictionary<string, object>();
    }

    /// <summary>
    /// Crea una excepción para fallo de autenticación (usuario no autenticado).
    /// </summary>
    /// <param name="validationType">Tipo de validación requerida.</param>
    /// <param name="endpointPath">Ruta del endpoint.</param>
    /// <param name="httpMethod">Método HTTP.</param>
    /// <param name="reason">Razón específica del fallo.</param>
    /// <returns>Excepción configurada para fallo de autenticación.</returns>
    public static AuthorizationMiddlewareException AuthenticationRequired(
        AuthorizationValidationTypes validationType,
        string endpointPath,
        string httpMethod,
        string? reason = null)
    {
        var message = $"Autenticación requerida para acceder a {httpMethod} {endpointPath}";
        if (!string.IsNullOrWhiteSpace(reason))
            message += $". Razón: {reason}";

        var exception = new AuthorizationMiddlewareException(
            validationType,
            endpointPath,
            httpMethod,
            message);

        exception.ContextData["FailureType"] = "AuthenticationRequired";
        if (reason != null)
            exception.ContextData["Reason"] = reason;

        return exception;
    }

    /// <summary>
    /// Crea una excepción para fallo de autorización (usuario autenticado pero sin permisos).
    /// </summary>
    /// <param name="validationType">Tipo de validación que falló.</param>
    /// <param name="endpointPath">Ruta del endpoint.</param>
    /// <param name="httpMethod">Método HTTP.</param>
    /// <param name="userId">ID del usuario autenticado.</param>
    /// <param name="requiredPermissions">Permisos requeridos que faltan.</param>
    /// <param name="reason">Razón específica del fallo.</param>
    /// <returns>Excepción configurada para fallo de autorización.</returns>
    public static AuthorizationMiddlewareException InsufficientPermissions(
        AuthorizationValidationTypes validationType,
        string endpointPath,
        string httpMethod,
        string userId,
        IList<string>? requiredPermissions = null,
        string? reason = null)
    {
        var message = $"Permisos insuficientes para usuario '{userId}' accediendo a {httpMethod} {endpointPath}";
        if (!string.IsNullOrWhiteSpace(reason))
            message += $". Razón: {reason}";

        var exception = new AuthorizationMiddlewareException(
            validationType,
            endpointPath,
            httpMethod,
            message,
            userId);

        exception.ContextData["FailureType"] = "InsufficientPermissions";
        exception.ContextData["UserId"] = userId;
        if (requiredPermissions?.Any() == true)
            exception.ContextData["RequiredPermissions"] = requiredPermissions.ToArray();
        if (reason != null)
            exception.ContextData["Reason"] = reason;

        return exception;
    }

    /// <summary>
    /// Crea una excepción para acceso denegado a funcionalidades de desarrollo.
    /// </summary>
    /// <param name="endpointPath">Ruta del endpoint.</param>
    /// <param name="httpMethod">Método HTTP.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="reason">Razón por la cual se requiere acceso de desarrollo.</param>
    /// <returns>Excepción configurada para acceso de desarrollo denegado.</returns>
    public static AuthorizationMiddlewareException DevelopmentAccessDenied(
        string endpointPath,
        string httpMethod,
        string? userId = null,
        string? reason = null)
    {
        var userInfo = string.IsNullOrWhiteSpace(userId) ? "usuario no autenticado" : $"usuario '{userId}'";
        var message = $"Acceso de desarrollo requerido para {userInfo} accediendo a {httpMethod} {endpointPath}";
        if (!string.IsNullOrWhiteSpace(reason))
            message += $". Razón: {reason}";

        var exception = new AuthorizationMiddlewareException(
            AuthorizationValidationTypes.DevelopmentAccess(reason),
            endpointPath,
            httpMethod,
            message,
            userId);

        exception.ContextData["FailureType"] = "DevelopmentAccessDenied";
        exception.ContextData["RequiredAccess"] = "Development";
        if (userId != null)
            exception.ContextData["UserId"] = userId;
        if (reason != null)
            exception.ContextData["Reason"] = reason;

        return exception;
    }

    /// <summary>
    /// Crea una excepción para acceso denegado a funcionalidades de SuperAdmin.
    /// </summary>
    /// <param name="endpointPath">Ruta del endpoint.</param>
    /// <param name="httpMethod">Método HTTP.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="reason">Razón por la cual se requiere acceso de SuperAdmin.</param>
    /// <returns>Excepción configurada para acceso de SuperAdmin denegado.</returns>
    public static AuthorizationMiddlewareException SuperAdminAccessDenied(
        string endpointPath,
        string httpMethod,
        string? userId = null,
        string? reason = null)
    {
        var userInfo = string.IsNullOrWhiteSpace(userId) ? "usuario no autenticado" : $"usuario '{userId}'";
        var message = $"Acceso de SuperAdmin requerido para {userInfo} accediendo a {httpMethod} {endpointPath}";
        if (!string.IsNullOrWhiteSpace(reason))
            message += $". Razón: {reason}";

        var exception = new AuthorizationMiddlewareException(
            AuthorizationValidationTypes.SuperAdminAccess(reason),
            endpointPath,
            httpMethod,
            message,
            userId);

        exception.ContextData["FailureType"] = "SuperAdminAccessDenied";
        exception.ContextData["RequiredAccess"] = "SuperAdmin";
        if (userId != null)
            exception.ContextData["UserId"] = userId;
        if (reason != null)
            exception.ContextData["Reason"] = reason;

        return exception;
    }

    /// <summary>
    /// Crea una excepción para errores de configuración de autorización.
    /// </summary>
    /// <param name="endpointPath">Ruta del endpoint.</param>
    /// <param name="httpMethod">Método HTTP.</param>
    /// <param name="configurationIssue">Descripción del problema de configuración.</param>
    /// <param name="innerException">Excepción interna que causó el problema.</param>
    /// <returns>Excepción configurada para error de configuración.</returns>
    public static AuthorizationMiddlewareException ConfigurationError(
        string endpointPath,
        string httpMethod,
        string configurationIssue,
        Exception? innerException = null)
    {
        var message = $"Error de configuración de autorización para {httpMethod} {endpointPath}: {configurationIssue}";

        var exception = new AuthorizationMiddlewareException(
            AuthorizationValidationTypes.StandardValidation(),
            endpointPath,
            httpMethod,
            message,
            innerException: innerException);

        // Los errores de configuración siempre son 500
        exception.ContextData["FailureType"] = "ConfigurationError";
        exception.ContextData["ConfigurationIssue"] = configurationIssue;

        return exception;
    }

    /// <summary>
    /// Determina el código de estado HTTP apropiado basándose en el tipo de validación.
    /// </summary>
    /// <param name="validationType">Tipo de validación de autorización.</param>
    /// <returns>Código de estado HTTP apropiado.</returns>
    private static HttpStatusCode DetermineStatusCode(AuthorizationValidationTypes validationType)
    {
        return validationType.Type switch
        {
            AuthorizationType.PermissionsNotRequired => HttpStatusCode.InternalServerError, // No debería llegar aquí
            AuthorizationType.DevelopmentAccess => HttpStatusCode.Forbidden,
            AuthorizationType.SuperAdminAccess => HttpStatusCode.Forbidden,
            AuthorizationType.ExplicitValidation => HttpStatusCode.Forbidden,
            AuthorizationType.StandardValidation => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError,
        };
    }

    /// <summary>
    /// Proporciona información detallada para logging y debugging.
    /// </summary>
    /// <returns>Diccionario con información contextual completa.</returns>
    public IDictionary<string, object> GetLoggingContext()
    {
        var context = new Dictionary<string, object>
        {
            ["Exception"] = GetType().Name,
            ["ValidationType"] = ValidationType.Type.ToString(),
            ["ValidationReason"] = ValidationType.Reason ?? "N/A",
            ["EndpointPath"] = EndpointPath,
            ["HttpMethod"] = HttpMethod,
            ["StatusCode"] = (int)StatusCode,
            ["Message"] = Message,
        };

        if (!string.IsNullOrWhiteSpace(UserId))
            context["UserId"] = UserId;

        // Agregar datos de contexto específicos
        foreach (var kvp in ContextData)
        {
            context[kvp.Key] = kvp.Value;
        }

        return context;
    }

    /// <summary>
    /// Representación string enriquecida para logging.
    /// </summary>
    public override string ToString()
    {
        var details = new List<string>
        {
            $"Tipo: {ValidationType}",
            $"Endpoint: {HttpMethod} {EndpointPath}",
            $"Status: {(int)StatusCode} {StatusCode}",
        };

        if (!string.IsNullOrWhiteSpace(UserId))
            details.Add($"Usuario: {UserId}");

        if (ContextData.Count > 0)
        {
            var contextInfo = string.Join(", ", ContextData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            details.Add($"Contexto: {contextInfo}");
        }

        return $"{Message}\nDetalles: {string.Join(" | ", details)}";
    }
}
