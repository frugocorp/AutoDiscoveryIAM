// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMServiceCollectionExtensions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;

/// <summary>
/// Registro de servicios del motor RBAC de AutoDiscoveryIAM: CORS, rate limiting, JWT bearer,
/// autorización por permisos, y toda la configuración (Options) que el motor necesita. Sube un nivel
/// el mismo patrón que ya existe por feature (<c>AddFeaturesDI()</c>).
/// </summary>
/// <remarks>
/// No registra <see cref="AutoDiscoveryIAMDbContext"/> -- eso lo decide quien instala el paquete, con el
/// proveedor de base de datos que elija y sus propias migraciones (ver README.md). Debe llamarse
/// <c>AddDbContext&lt;AutoDiscoveryIAMDbContext&gt;(...)</c> **antes** de <see cref="AddAutoDiscoveryIAM"/>,
/// porque el health check de este método depende de que ya esté registrado.
/// Sí registra la generación del documento OpenAPI (<c>AddOpenApi</c> nativo de
/// <c>Microsoft.AspNetCore.OpenApi</c>) con los transformers que hacen funcionar
/// <see cref="EndpointOperationDocs"/> -- sin esto, la documentación que cada slice escribe en su
/// <c>Docs/&lt;Slice&gt;OperationDocs.cs</c> nunca aparecería en el JSON generado. Esto solo registra
/// servicios: no expone ningún endpoint HTTP por sí solo ni fuerza ningún renderizador -- publicar
/// el documento (<c>app.MapOpenApi(...)</c>) y elegir cómo mostrarlo (Swagger UI, Scalar, ninguno)
/// sigue siendo 100% decisión del proyecto consumidor.
/// </remarks>
public static class AutoDiscoveryIAMServiceCollectionExtensions
{
  public static IServiceCollection AddAutoDiscoveryIAM(
    this IServiceCollection services,
    IConfiguration configuration,
    string openApiDocumentName = "v1",
    Action<OpenApiOptions>? configureOpenApi = null)
  {
    services.AddTransient(typeof(ILoggerBase<>), typeof(LoggerBase<>));
    services.AddSingleton<IPasswordHasher, PasswordHasher>();
    services.AddScoped<ICurrentAuditUserService, CurrentAuditUserService>();
    services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();
    services.AddSingleton<IJwtBlacklistService, JwtBlacklistService>();
    services.AddSingleton<IEndpointPermissionKeyResolver, EndpointPermissionKeyResolver>();

    services.AddFeaturesDI();
    services.AddNotificationsDI();

    services.AddHttpContextAccessor();

    services.AddScoped<IAutoDiscoveryIAMUnitOfWork, AutoDiscoveryIAMUnitOfWork>();

    // Health check: permite a un orquestador (Docker/K8s) detectar si la app puede
    // hablar con la base de datos, sin necesitar un JWT (endpoint publico via PermissionsNotRequired).
    services.AddHealthChecks()
      .AddDbContextCheck<AutoDiscoveryIAMDbContext>();

    AddSettings(services, configuration);

    AddJwtAuthentication(services, configuration);
    AddRateLimiting(services, configuration);
    AddForwardedHeaders(services, configuration);
    AddSecretProtection(services);
    AddCors(services, configuration);
    AddOpenApiDocument(services, openApiDocumentName, configureOpenApi);

    return services;
  }

  private static void AddSettings(IServiceCollection services, IConfiguration configuration)
  {
    // JwtSettings: Key/Issuer/Audience son propios de cada instalación, sin default posible --
    // validados al arranque para fallar rápido en vez de dejar el login silenciosamente roto.
    services.AddOptions<JwtSettings>()
      .Bind(configuration.GetSection("JwtSettings"))
      .ValidateOnStart();
    services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

    // Secciones de seguridad con valores por defecto, pero validadas al arranque: un valor fuera de
    // rango (bloqueo con 0 intentos, código 2FA de 3 dígitos, refresh token corto) falla de inmediato.
    services.AddOptions<RefreshTokenSettings>().Bind(configuration.GetSection("RefreshTokenSettings")).ValidateOnStart();
    services.AddSingleton<IValidateOptions<RefreshTokenSettings>, RefreshTokenSettingsValidator>();
    services.Configure<GoogleAuthSettings>(configuration.GetSection("Authentication:Google"));
    services.Configure<ExternalLoginSettings>(configuration.GetSection("Authentication"));
    services.Configure<MailSettings>(configuration.GetSection("Mail"));
    services.Configure<ActivationSettings>(configuration.GetSection("Activation"));
    services.Configure<RegistrationSettings>(configuration.GetSection(RegistrationSettings.SectionName));
    services.Configure<PasswordResetSettings>(configuration.GetSection("PasswordReset"));
    services.AddOptions<LockoutSettings>().Bind(configuration.GetSection("Lockout")).ValidateOnStart();
    services.AddSingleton<IValidateOptions<LockoutSettings>, LockoutSettingsValidator>();
    services.AddOptions<TwoFactorSettings>().Bind(configuration.GetSection("TwoFactor")).ValidateOnStart();
    services.AddSingleton<IValidateOptions<TwoFactorSettings>, TwoFactorSettingsValidator>();

    // Política de contraseñas configurable  
    services.AddOptions<PasswordPolicyOptions>()
      .Bind(configuration.GetSection("PasswordStruct"))
      .ValidateOnStart();
    services.AddSingleton<IValidateOptions<PasswordPolicyOptions>, PasswordPolicyOptionsValidator>();
  }

  private static void AddOpenApiDocument(IServiceCollection services, string openApiDocumentName, Action<OpenApiOptions>? configureOpenApi)
  {
    services.AddOpenApi(openApiDocumentName, options =>
    {
      // Evita colisiones de SchemaId entre DTOs con el mismo nombre simple en distintos slices
      // (equivalente nativo de Swashbuckle CustomSchemaIds(type => type.FullName)).
      options.CreateSchemaReferenceId = typeInfo => typeInfo.Type.FullName;

      // Metadata del documento (título, versión, contacto) y esquema de seguridad Bearer.
      options.AddDocumentTransformer<ApiDocumentInfoTransformer>();

      // Fuente única de verdad para las descripciones de tags -- se declaran una sola vez aquí en
      // vez de repetirse por endpoint.
      options.AddDocumentTransformer<DocumentTagDescriptionsTransformer>();

      // Aplica la EndpointOperationDocs que cada endpoint adjunta vía WithMetadata(...) -- sin este
      // transformer, esa documentación por endpoint nunca llega al JSON generado.
      options.AddOperationTransformer<EndpointOperationDocsTransformer>();

      // El proyecto consumidor puede agregar sus propios transformers (o fijar OpenApiVersion si su
      // renderizador lo necesita, ver README.md) sobre el mismo documento, sin pisar lo de arriba.
      configureOpenApi?.Invoke(options);
    });
  }

  private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
  {
    var jwtSettings = configuration.GetSection("JwtSettings");
    var key = new JwtSettings { Key = jwtSettings["Key"] ?? throw new InvalidOperationException("Clave JWT (Key) no encontrada en la configuración.") }.GetSigningKeyBytes();

    services.AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
      options.RequireHttpsMetadata = false; // Poner a true si tu API usa HTTPS directamente (no solo Nginx)
      options.SaveToken = true;
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true, // Poner a true si quieres validar el emisor (issuer)
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true, // Poner a true si quieres validar la audiencia (audience)
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        // Solo el algoritmo con el que la librería firma: ningún otro (ni "none") se acepta.
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256, SecurityAlgorithms.HmacSha256Signature],
        ClockSkew = TimeSpan.Zero, // Opcional: elimina el retardo cuando el token expira
      };

      // Personaliza la respuesta 401 y rechaza tokens invalidados (logout/cambio de contraseña).
      options.Events = new JwtBearerEvents
      {
        OnChallenge = HandleJwtChallenge,
        OnTokenValidated = HandleJwtTokenValidatedAsync,
      };
    });

    services.AddAuthorization();

    // Almacén de JwtBlacklistService (jti invalidados por logout). TryAdd: si el consumidor ya
    // registró un caché distribuido compartido (Redis, SQL Server), se usa ese.
    services.AddDistributedMemoryCache();
  }

  private static Task HandleJwtChallenge(JwtBearerChallengeContext context)
  {
    context.HandleResponse(); // Evita la respuesta predeterminada

    var response = new ApiResponse<object>(
            false,
            null,
            "No autorizado. Debe autenticarse para acceder a este recurso.",
            ErrorInfo.Status401Unauthorized);

    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    context.Response.ContentType = "application/json";
    var json = System.Text.Json.JsonSerializer.Serialize(response);
    return context.Response.WriteAsync(json);
  }

  /// <summary>
  /// Rechaza tokens cuyo jti fue invalidado explícitamente (logout), de una cuenta eliminada o
  /// inactiva, o emitidos antes del último cambio de contraseña del usuario (claim tvf vs.
  /// User.TokenValidFrom), antes de su expiración natural.
  /// </summary>
  private static async Task HandleJwtTokenValidatedAsync(TokenValidatedContext context)
  {
    var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

    if (!string.IsNullOrEmpty(jti)
      && await context.HttpContext.RequestServices.GetRequiredService<IJwtBlacklistService>().IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
    {
      RejectToken(context, "Token con jti invalidado (blacklist).", "Token invalidado.");
      return;
    }

    var tokenValidFromClaim = context.Principal?.FindFirst(JwtClaimTypes.TokenValidFrom)?.Value;
    var publicIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (!long.TryParse(tokenValidFromClaim, out var tokenValidFromUnixSeconds) || !Guid.TryParse(publicIdClaim, out var publicId))
    {
      return;
    }

    var dbContext = context.HttpContext.RequestServices.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var currentUser = await dbContext.Users
      .Where(u => u.PublicId == publicId)
      .Select(u => new { u.TokenValidFrom, u.IsActive })
      .FirstOrDefaultAsync();

    // Cuenta eliminada o desactivada: el token deja de valer en todos los endpoints, incluidos los
    // self-service ([PermissionsNotRequired] + RequireAuthorization), que no pasan por el RBAC.
    if (currentUser is null || !currentUser.IsActive)
    {
      RejectToken(context, "Token de una cuenta eliminada o inactiva.", "Cuenta eliminada o inactiva.");
      return;
    }

    var currentTokenValidFromUnixSeconds = new DateTimeOffset(TimeHelpers.ConvertMexicoTimeToUtc(currentUser.TokenValidFrom), TimeSpan.Zero).ToUnixTimeSeconds();

    if (tokenValidFromUnixSeconds < currentTokenValidFromUnixSeconds)
    {
      RejectToken(context, "Token emitido antes del último cambio de contraseña (tvf desactualizado).", "Token invalidado por cambio de contraseña.");
    }
  }

  private static void RejectToken(TokenValidatedContext context, string securityEventDetail, string failureMessage)
  {
    var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    var securityEventLogger = context.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>();
    securityEventLogger.LogSecurityEvent(
      SecurityEventType.AccessTokenRejectedBlacklisted,
      email,
      context.HttpContext.Connection.RemoteIpAddress?.ToString(),
      context.HttpContext.Request.Path,
      securityEventDetail);
    context.Fail(failureMessage);
  }

  private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
  {
    // Limitador nativo de ASP.NET Core. Las reglas de la sección se completan con los límites por
    // defecto de las rutas sensibles de la librería (ver AutoDiscoveryIAMRateLimitDefaults):
    // protegidas aunque la sección no exista. Cada cliente es su IP de conexión, nunca un header.
    services.AddOptions<RateLimitingSettings>()
      .Bind(configuration.GetSection(RateLimitingSettings.SectionName))
      .PostConfigure(AutoDiscoveryIAMRateLimitDefaults.Apply)
      .ValidateOnStart();
    services.AddSingleton<IValidateOptions<RateLimitingSettings>, RateLimitingSettingsValidator>();
    services.AddRateLimiter(_ => { });
    services.AddOptions<RateLimiterOptions>()
      .Configure<IOptions<RateLimitingSettings>>((options, settings) => AutoDiscoveryIAMRateLimiter.Configure(options, settings.Value));
  }

  private static void AddForwardedHeaders(IServiceCollection services, IConfiguration configuration)
  {
    // Sin esto, UseForwardedHeaders() nunca lee X-Forwarded-For/X-Forwarded-Proto (Request.IsHttps y
    // Connection.RemoteIpAddress verían siempre los del proxy). Solo se aceptan de los proxies de
    // confianza de la sección ForwardedHeaders (por defecto, la red bridge de Docker).
    services.Configure<TrustedProxySettings>(configuration.GetSection("ForwardedHeaders"));
    services.AddOptions<ForwardedHeadersOptions>()
      .Configure<IOptions<TrustedProxySettings>>((options, trustedProxies) => TrustedProxySettings.Apply(trustedProxies.Value, options));
  }

  private static void AddSecretProtection(IServiceCollection services)
  {
    // Cifra los secretos de Authenticator (TotpService). Idempotente: el consumidor configura dónde
    // persiste el anillo de claves con su propio AddDataProtection().PersistKeysTo...(), que tiene
    // que sobrevivir a reinicios y ser compartido entre instancias.
    services.AddDataProtection();
  }

  private static void AddCors(IServiceCollection services, IConfiguration configuration)
  {
    // Los orígenes permitidos se leen de configuración (Cors:AllowedOrigins), nunca AllowAnyOrigin.
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    // Con el refresh token en cookie, el navegador solo la manda a otro origen si CORS permite
    // credenciales. Es seguro porque los orígenes siempre son explícitos.
    var refreshTokenInCookie = configuration.GetValue("RefreshTokenSettings:Delivery", RefreshTokenDeliveryMode.Body) != RefreshTokenDeliveryMode.Body;
    services.AddCors(options =>
    {
      options.AddPolicy(
          "AllowSpecificOrigins",
          policyBuilder =>
          {
            policyBuilder.WithOrigins(allowedOrigins)
                           .AllowAnyMethod()
                           .AllowAnyHeader();
            if (refreshTokenInCookie)
            {
              policyBuilder.AllowCredentials();
            }
          });
    });
  }
}
