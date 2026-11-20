using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;

/// <summary>
/// Servicio para gestionar el contexto de autenticación del usuario actual.
/// Extrae información del JWT sin dependencias de otros dominios.
/// </summary>
public interface IAuthContextService
{
    /// <summary>
    /// Obtiene el usuario completo del contexto actual (incluye consulta a BD).
    /// </summary>
    /// <returns>
    /// El usuario autenticado completo o null si no está autenticado.
    /// </returns>
    Task<User?> GetCurrentUserAsync();

    /// <summary>
    /// Obtiene el PublicId del usuario actual desde los claims del JWT.
    /// Método síncrono que no realiza consultas a BD - SEGURO.
    /// </summary>
    /// <returns>
    /// El PublicId del usuario autenticado o null si no está autenticado.
    /// </returns>
    Guid? GetCurrentUserPublicId();

    /// <summary>
    /// Obtiene el email del usuario actual desde los claims del JWT.
    /// Método síncrono que no realiza consultas a BD.
    /// </summary>
    /// <returns>
    /// El email del usuario autenticado o null si no está autenticado.
    /// </returns>
    string? GetCurrentUserEmail();

    /// <summary>
    /// Verifica si el usuario actual está autenticado.
    /// </summary>
    /// <returns>
    /// true si el usuario está autenticado, false en caso contrario.
    /// </returns>
    bool IsAuthenticated();

    /// <summary>
    /// Obtiene todos los claims del usuario actual.
    /// </summary>
    /// <returns>
    /// Los claims del JWT del usuario autenticado o una colección vacía.
    /// </returns>
    IEnumerable<Claim> GetCurrentUserClaims();
}
