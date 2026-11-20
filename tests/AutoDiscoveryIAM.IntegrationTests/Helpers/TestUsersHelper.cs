using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

/// <summary>
/// Helper centralizado para manejo de usuarios de test y autenticación.
/// </summary>
public static class TestUsersHelper
{
    /// <summary>
    /// Obtiene todos los tipos de usuarios disponibles para testing.
    /// </summary>
    public static IEnumerable<TestUser> GetAllTestUsers()
    {
        return new[]
        {
            new TestUser("Anonymous", null, null, "Usuario anónimo sin autenticación"),
            new TestUser("Regular", "user@example.com", "UserPass123!", "Usuario regular con permisos básicos"),
            new TestUser("Admin", "admin@example.com", "AdminPass123!", "Usuario administrador"),
            new TestUser("SuAdmin", "su@example.com", "UserPass123!", "Super administrador con acceso completo"),
            new TestUser("Development", "dev@example.com", "AdminPass123!", "Usuario de desarrollo con acceso especial"),
        };
    }

    /// <summary>
    /// Obtiene un usuario específico por tipo.
    /// </summary>
    public static TestUser GetUserByType(UserType userType)
    {
        return userType switch
        {
            UserType.Anonymous => GetAllTestUsers().First(u => u.Type == "Anonymous"),
            UserType.Regular => GetAllTestUsers().First(u => u.Type == "Regular"),
            UserType.Admin => GetAllTestUsers().First(u => u.Type == "Admin"),
            UserType.SuAdmin => GetAllTestUsers().First(u => u.Type == "SuAdmin"),
            UserType.Development => GetAllTestUsers().First(u => u.Type == "Development"),
            _ => throw new ArgumentOutOfRangeException(nameof(userType)),
        };
    }

    /// <summary>
    /// Configura autenticación para un usuario específico.
    /// </summary>
    public static async Task<AuthenticatedTestUser> AuthenticateUserAsync(
        this HttpClient client,
        TestUser user,
        CancellationToken cancellationToken = default)
    {
        if (user.Email == null)
        {
            // Usuario anónimo - remover autenticación
            client.DefaultRequestHeaders.Authorization = null;
            return new AuthenticatedTestUser(user, null);
        }

        // Login y obtener token
        var loginRequest = new LoginRequestDto(user.Email, user.Password!);
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest, cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
        var token = loginContent!.Data!.Token;

        // Configurar header de autorización
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return new AuthenticatedTestUser(user, token);
    }

    /// <summary>
    /// Configura autenticación por tipo de usuario.
    /// </summary>
    public static async Task<AuthenticatedTestUser> AuthenticateUserAsync(
        this HttpClient client,
        UserType userType,
        CancellationToken cancellationToken = default)
    {
        var user = GetUserByType(userType);
        return await client.AuthenticateUserAsync(user, cancellationToken);
    }

    /// <summary>
    /// Remueve autenticación del client (modo anónimo).
    /// </summary>
    public static void RemoveAuthentication(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Obtiene token de autenticación para credenciales específicas (método de bajo nivel).
    /// NOTA: Este método solo retorna el token, NO configura el header Authorization.
    /// Para configurar automáticamente el header, usar AuthenticateUserAsync() en su lugar.
    /// </summary>
    public static async Task<string> GetAuthTokenAsync(
        this HttpClient client,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var loginRequest = new
        {
            Email = email,
            Password = password,
        };

        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest, cancellationToken);

        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Login failed para {email}: {loginResponse.StatusCode} - {errorContent}");
        }

        var loginContent = await loginResponse.Content.ReadAsStringAsync(cancellationToken);

        // Extract token from JSON response
        const string tokenProperty = "\"token\":\"";
        var tokenStart = loginContent.IndexOf(tokenProperty, StringComparison.OrdinalIgnoreCase);

        if (tokenStart < 0)
        {
            throw new InvalidOperationException($"Token not found in login response: {loginContent}");
        }

        tokenStart += tokenProperty.Length;
        var tokenEnd = loginContent.IndexOf('"', tokenStart);

        if (tokenEnd < 0)
        {
            throw new InvalidOperationException($"Invalid token format in login response: {loginContent}");
        }

        return loginContent[tokenStart..tokenEnd];
    }

    /// <summary>
    /// Obtiene token de autenticación para un tipo de usuario específico.
    /// NOTA: Este método solo retorna el token, NO configura el header Authorization.
    /// Para configurar automáticamente el header, usar AuthenticateUserAsync() en su lugar.
    /// </summary>
    public static async Task<string> GetAuthTokenAsync(
        this HttpClient client,
        UserType userType,
        CancellationToken cancellationToken = default)
    {
        var user = GetUserByType(userType);

        if (user.Email == null)
        {
            throw new InvalidOperationException("Cannot get auth token para anonymous user");
        }

        return await client.GetAuthTokenAsync(user.Email, user.Password!, cancellationToken);
    }

    /// <summary>
    /// ✅ MEJORA: Obtiene solo usuarios Development y SuAdmin (para tests específicos).
    /// Ejemplo: "SuAdmin endpoints deben permitir TAMBIÉN Development users"
    /// </summary>
    public static IEnumerable<TestUser> GetDevelopmentAndSuAdminUsers()
    {
        return GetAllTestUsers().Where(u => u.Type == "Development" || u.Type == "SuAdmin");
    }

    /// <summary>
    /// ✅ MEJORA: Obtiene solo usuarios Regular y Admin (para tests de denegación).
    /// Ejemplo: "Regular y Admin users NO deben acceder endpoints SuAdmin"
    /// </summary>
    public static IEnumerable<TestUser> GetRegularAndAdminUsers()
    {
        return GetAllTestUsers().Where(u => u.Type == "Regular" || u.Type == "Admin");
    }

    /// <summary>
    /// ✅ MEJORA: Obtiene solo usuario Anonymous (para tests de autenticación).
    /// </summary>
    public static TestUser GetAnonymousUser()
    {
        return GetAllTestUsers().First(u => u.Type == "Anonymous");
    }

    /// <summary>
    /// ✅ MEJORA: Obtiene usuarios que NO deberían tener acceso según el endpoint.
    /// Uso específico según el test, no matriz completa.
    /// </summary>
    public static IEnumerable<TestUser> GetUnauthorizedUsersFor(EndpointAuthorizationType endpointType)
    {
        return endpointType switch
        {
            EndpointAuthorizationType.PermissionsNotRequired =>
                Enumerable.Empty<TestUser>(), // Todos pueden acceder

            EndpointAuthorizationType.RequiresSuAdminAccess =>
                GetAllTestUsers().Where(u => u.Type != "SuAdmin" && u.Type != "Development"),

            EndpointAuthorizationType.RequiresDevelopmentAccess =>
                GetAllTestUsers().Where(u => u.Type != "Development"),

            EndpointAuthorizationType.RequiresRegularPermissions =>
                GetAllTestUsers().Where(u => u.Type == "Anonymous"), // Solo anonymous debería ser denegado

            _ => throw new ArgumentOutOfRangeException(nameof(endpointType)),
        };
    }

    /// <summary>
    /// ✅ MEJORA: Obtiene usuarios que SÍ deberían tener acceso según el endpoint.
    /// Uso específico según el test, no matriz completa.
    /// </summary>
    public static IEnumerable<TestUser> GetAuthorizedUsersFor(EndpointAuthorizationType endpointType)
    {
        return endpointType switch
        {
            EndpointAuthorizationType.PermissionsNotRequired =>
                GetAllTestUsers(), // Todos pueden acceder

            EndpointAuthorizationType.RequiresSuAdminAccess =>
                GetAllTestUsers().Where(u => u.Type == "SuAdmin" || u.Type == "Development"),

            EndpointAuthorizationType.RequiresDevelopmentAccess =>
                GetAllTestUsers().Where(u => u.Type == "Development"),

            EndpointAuthorizationType.RequiresRegularPermissions =>
                GetAllTestUsers().Where(u => u.Type != "Anonymous"), // Todos excepto anonymous

            _ => throw new ArgumentOutOfRangeException(nameof(endpointType)),
        };
    }
}

/// <summary>
/// Representa un usuario de test.
/// </summary>
public record TestUser(
    string Type,
    string? Email,
    string? Password,
    string Description)
{
    public bool IsAnonymous => Email == null;
    public bool IsAuthenticated => !IsAnonymous;
}

/// <summary>
/// Representa un usuario autenticado en el sistema.
/// </summary>
public record AuthenticatedTestUser(
    TestUser User,
    string? Token)
{
    public bool IsAnonymous => User.IsAnonymous;
    public string Type => User.Type;
    public string? Email => User.Email;
}

/// <summary>
/// Tipos de usuarios para testing.
/// </summary>
public enum UserType
{
    Anonymous,
    Regular,
    Admin,
    SuAdmin,
    Development,
}

/// <summary>
/// Tipos de autorización de endpoints.
/// </summary>
public enum EndpointAuthorizationType
{
    PermissionsNotRequired,
    RequiresSuAdminAccess,
    RequiresDevelopmentAccess,
    RequiresRegularPermissions,
}
