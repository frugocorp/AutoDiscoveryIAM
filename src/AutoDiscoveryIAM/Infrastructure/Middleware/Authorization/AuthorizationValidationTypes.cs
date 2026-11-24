// -----------------------------------------------------------------------
// <copyright file="AuthorizationValidationTypes.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Representa los diferentes tipos de validación de autorización disponibles en el sistema.
/// Proporciona una abstracción type-safe para manejar los distintos enfoques de autorización
/// basados en atributos descubiertos por reflexión.
/// </summary>
/// <remarks>
/// Esta clase encapsula la lógica de validación de autorización a partir de datos en la BD,
/// poblados automáticamente por el sistema de descubrimiento de permisos.
///
/// Tipos soportados:
/// - PermissionsNotRequired: Endpoints que no requieren validación de permisos
/// - DevelopmentAccess: Endpoints que requieren acceso de desarrollo
/// - SuperAdminAccess: Endpoints que requieren acceso de SuperAdmin
/// - ExplicitValidation: Endpoints con validación explícita de permisos
/// - StandardValidation: Endpoints con validación estándar (por defecto)
/// </remarks>
public sealed class AuthorizationValidationTypes
{
    /// <summary>
    /// Tipo de validación de autorización.
    /// </summary>
    public AuthorizationType Type { get; }

    /// <summary>
    /// Razón o nota asociada con este tipo de validación.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Indica si este tipo requiere omitir la validación de permisos.
    /// </summary>
    public bool SkipsPermissionValidation => Type == AuthorizationType.PermissionsNotRequired;

    /// <summary>
    /// Indica si este tipo requiere acceso administrativo especial.
    /// </summary>
    public bool RequiresSpecialAdminAccess => Type is AuthorizationType.DevelopmentAccess or AuthorizationType.SuperAdminAccess;

    /// <summary>
    /// Indica si este tipo utiliza validación estándar de permisos.
    /// </summary>
    public bool UsesStandardValidation => Type is AuthorizationType.StandardValidation or AuthorizationType.ExplicitValidation;

    /// <summary>
    /// Constructor privado para garantizar creación a través de factory methods.
    /// </summary>
    private AuthorizationValidationTypes(AuthorizationType type, string? reason = null)
    {
        Type = type;
        Reason = reason;
    }

    /// <summary>
    /// Crea un tipo de validación para endpoints que no requieren permisos.
    /// </summary>
    /// <param name="reason">Razón por la cual no se requieren permisos.</param>
    /// <returns>Instancia configurada para omitir validación de permisos.</returns>
    public static AuthorizationValidationTypes PermissionsNotRequired(string? reason = null)
        => new(AuthorizationType.PermissionsNotRequired, reason);

    /// <summary>
    /// Crea un tipo de validación para endpoints que requieren acceso de desarrollo.
    /// </summary>
    /// <param name="reason">Razón por la cual se requiere acceso de desarrollo.</param>
    /// <returns>Instancia configurada para validación de acceso de desarrollo.</returns>
    public static AuthorizationValidationTypes DevelopmentAccess(string? reason = null)
        => new(AuthorizationType.DevelopmentAccess, reason);

    /// <summary>
    /// Crea un tipo de validación para endpoints que requieren acceso de SuperAdmin.
    /// </summary>
    /// <param name="reason">Razón por la cual se requiere acceso de SuperAdmin.</param>
    /// <returns>Instancia configurada para validación de acceso de SuperAdmin.</returns>
    public static AuthorizationValidationTypes SuperAdminAccess(string? reason = null)
        => new(AuthorizationType.SuperAdminAccess, reason);

    /// <summary>
    /// Crea un tipo de validación explícita de permisos.
    /// </summary>
    /// <param name="note">Nota explicativa sobre la validación explícita.</param>
    /// <returns>Instancia configurada para validación explícita de permisos.</returns>
    public static AuthorizationValidationTypes ExplicitValidation(string? note = null)
        => new(AuthorizationType.ExplicitValidation, note);

    /// <summary>
    /// Crea un tipo de validación estándar (comportamiento por defecto).
    /// </summary>
    /// <returns>Instancia configurada para validación estándar de permisos.</returns>
    public static AuthorizationValidationTypes StandardValidation()
        => new(AuthorizationType.StandardValidation);

    /// <summary>
    /// Factory method que determina el tipo de validación basándose en los campos
    /// de autorización de un permiso descubierto por reflexión.
    /// </summary>
    /// <param name="hasPermissionsNotRequired">Indica si tiene atributo PermissionsNotRequired.</param>
    /// <param name="permissionsNotRequiredReason">Razón del atributo PermissionsNotRequired.</param>
    /// <param name="requiresDevelopmentAccess">Indica si requiere acceso de desarrollo.</param>
    /// <param name="developmentAccessReason">Razón del acceso de desarrollo.</param>
    /// <param name="requiresSuAdminAccess">Indica si requiere acceso de SuperAdmin.</param>
    /// <param name="suAdminAccessReason">Razón del acceso de SuperAdmin.</param>
    /// <param name="hasExplicitValidation">Indica si tiene validación explícita.</param>
    /// <param name="explicitValidationNote">Nota de la validación explícita.</param>
    /// <returns>Tipo de validación apropiado basado en los parámetros.</returns>
    /// <exception cref="InvalidOperationException">Si se detecta configuración conflictiva de atributos.</exception>
    public static AuthorizationValidationTypes FromPermissionData(
        bool hasPermissionsNotRequired,
        string? permissionsNotRequiredReason,
        bool requiresDevelopmentAccess,
        string? developmentAccessReason,
        bool requiresSuAdminAccess,
        string? suAdminAccessReason,
        bool hasExplicitValidation,
        string? explicitValidationNote)
    {
        // Validar conflictos mutuamente excluyentes
        var activeFlags = new[]
        {
            hasPermissionsNotRequired,
            requiresDevelopmentAccess,
            requiresSuAdminAccess,
        }.Count(flag => flag);

        if (activeFlags > 1)
        {
            throw new InvalidOperationException(
                "Un endpoint no puede tener múltiples tipos de autorización conflictivos. " +
                $"Detectados: PermissionsNotRequired={hasPermissionsNotRequired}, " +
                $"DevelopmentAccess={requiresDevelopmentAccess}, " +
                $"SuperAdminAccess={requiresSuAdminAccess}");
        }

        // Determinar tipo basado en prioridad (orden de evaluación importa)
        if (hasPermissionsNotRequired)
            return PermissionsNotRequired(permissionsNotRequiredReason);

        if (requiresSuAdminAccess)
            return SuperAdminAccess(suAdminAccessReason);

        if (requiresDevelopmentAccess)
            return DevelopmentAccess(developmentAccessReason);

        if (hasExplicitValidation)
            return ExplicitValidation(explicitValidationNote);

        // Por defecto: validación estándar
        return StandardValidation();
    }

    /// <summary>
    /// Representación legible del tipo de validación.
    /// </summary>
    public override string ToString()
    {
        var typeDescription = Type switch
        {
            AuthorizationType.PermissionsNotRequired => "Sin validación de permisos",
            AuthorizationType.DevelopmentAccess => "Acceso de desarrollo",
            AuthorizationType.SuperAdminAccess => "Acceso de SuperAdmin",
            AuthorizationType.ExplicitValidation => "Validación explícita",
            AuthorizationType.StandardValidation => "Validación estándar",
            _ => Type.ToString(),
        };

        return string.IsNullOrWhiteSpace(Reason)
            ? typeDescription
            : $"{typeDescription}: {Reason}";
    }

    /// <summary>
    /// Constantes para mensajes de error de autorización localizados.
    /// </summary>
    public static class Messages
    {
        /// <summary>
        /// Mensaje para cuando se requiere autenticación.
        /// </summary>
        public const string AuthenticationRequired = "Se requiere autenticación para acceder a este recurso";

        /// <summary>
        /// Mensaje para cuando el contexto de usuario es inválido.
        /// </summary>
        public const string InvalidUserContext = "Contexto de usuario inválido - no se pudo obtener la información del usuario";

        /// <summary>
        /// Mensaje para cuando se requiere acceso de desarrollo.
        /// </summary>
        public const string DevelopmentAccessRequired = "Se requiere acceso de desarrollo para este recurso";

        /// <summary>
        /// Mensaje para cuando se requiere acceso de SuperAdmin.
        /// </summary>
        public const string SuperAdminAccessRequired = "Se requiere acceso de SuperAdmin para este recurso";

        /// <summary>
        /// Mensaje para cuando hay permisos insuficientes.
        /// </summary>
        public const string InsufficientPermissions = "Permisos insuficientes para acceder a este recurso";

        /// <summary>
        /// Mensaje para cuando no se puede determinar el permiso requerido.
        /// </summary>
        public const string PermissionValidationFailed = "La validación de permisos falló - no se pudo determinar el permiso requerido";

        /// <summary>
        /// Mensaje para cuando el usuario está autenticado pero inactivo.
        /// </summary>
        public const string UserInactive = "El usuario autenticado está inactivo y no puede acceder a este recurso";

        public const string PasswordChangeRequired = "Debes cambiar la contraseña que te asignó un administrador antes de usar este recurso (POST /users/me/password)";

        /// <summary>
        /// Template para mensaje de permiso específico requerido.
        /// Uso: string.Format(PermissionRequired, "nombre_permiso")
        /// </summary>
        public const string PermissionRequired = "Permisos insuficientes. Requerido: {0}";
    }
}

/// <summary>
/// Enumeración de los tipos de autorización disponibles.
/// </summary>
public enum AuthorizationType
{
    /// <summary>
    /// Validación estándar de permisos (comportamiento por defecto).
    /// </summary>
    StandardValidation = 0,

    /// <summary>
    /// No se requiere validación de permisos.
    /// </summary>
    PermissionsNotRequired = 1,

    /// <summary>
    /// Requiere acceso de desarrollo.
    /// </summary>
    DevelopmentAccess = 2,

    /// <summary>
    /// Requiere acceso de SuperAdmin.
    /// </summary>
    SuperAdminAccess = 3,

    /// <summary>
    /// Validación explícita de permisos.
    /// </summary>
    ExplicitValidation = 4,
}