// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.SampleHost.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

// *******************************************
// 1. Crear WebApplicationBuilder
// *******************************************
var builder = WebApplication.CreateBuilder(args);

// Cargar configuración desde appsettings.json y variables de entorno
var configuration = builder.Configuration;

// *******************************************
// 2. Configurar Servicios (Inyección de Dependencias)
// *******************************************

// Configurar Entity Framework Core DbContext -- el proveedor y la connection string se deciden
// aquí, no dentro de AutoDiscoveryIAMDbContext, para que un proyecto consumidor pueda elegir su propio
// proveedor de base de datos. La librería no trae migraciones empaquetadas: MigrationsAssembly
// apunta a este mismo proyecto (el "consumidor de referencia"), donde vive Migrations/ con las
// migraciones que genera este host -- cualquier proyecto real hace lo mismo con su propio
// ensamblado (ver README.md).
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
  throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada en la configuración.");
}

builder.Services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options.UseSqlServer(
  connectionString,
  sql => sql.MigrationsAssembly("FrugoCorp.AspNetCore.AutoDiscoveryIAM.SampleHost")));

builder.Services.AddSingleton<IConfiguration>(configuration);

// Motor RBAC de AutoDiscoveryIAM: CORS, rate limiting, JWT bearer, autorización por permisos,
// AutoDiscoveryIAMUnitOfWork, y toda la configuración (Options) que el motor necesita -- incluye
// el documento OpenAPI con los transformers que aplican EndpointOperationDocs (ver
// Infrastructure/Composition/AutoDiscoveryIAMServiceCollectionExtensions.cs). Requiere que
// AddDbContext<AutoDiscoveryIAMDbContext>(...) (arriba) ya se haya llamado.
builder.Services.AddAutoDiscoveryIAM(configuration, configureOpenApi: options =>
{
  // Swagger UI (swagger-ui-dist embebido en Swashbuckle.AspNetCore 10.2.3) no renderiza "openapi:
  // 3.1.x" todavía -- confirmado en vivo. Microsoft.OpenApi 2.x sigue siendo capaz de emitir 3.1;
  // esto solo fija qué versión de spec se sirve. Específico de este renderizador (Swagger UI, ver
  // app.UseSwaggerUI más abajo): un proyecto con otro renderizador (Scalar/ReDoc/Mintlify/etc.) no
  // necesita este ajuste.
  options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

// --- Espacio para servicios propios de este proyecto (no del motor RBAC) ---
// builder.Services.AddApplicationServices();
// --- Fin Espacio ---
builder.Services.AddEndpointsApiExplorer(); // Requerido para APIs mínimas si las usas

// *******************************************
// 3. Construir WebApplication
// *******************************************
var app = builder.Build();
#if DEBUG
app.MapExceptionHandlingMiddlewareDiagnostics();
#endif

// *******************************************
// 4. Configurar Pipeline de Peticiones HTTP (Middleware)
// *******************************************

// Debe ir primero: reescribe Request.Scheme/Connection.RemoteIpAddress usando los headers que
// envía Nginx (X-Forwarded-Proto/X-Forwarded-For), antes de que cualquier otro middleware los lea.
app.UseForwardedHeaders();

// Documento OpenAPI (generado nativamente, ver AddOpenApi arriba)
// Ruta pública y estable del documento generado -- "openapi", no "swagger": no lo genera
// Swashbuckle, así que el nombre no debe sugerir esa dependencia. Se fija
// explícitamente (en vez de aceptar el default del paquete) porque otros proyectos que instalen el
// futuro empaquetado NuGet necesitarán esta ruta para fusionar este documento con el
// suyo propio y exponer el 100% de los endpoints -- ver README de ese futuro paquete.
app.MapOpenApi("/openapi/{documentName}.json")
  .WithMetadata(new PermissionsNotRequiredAttribute("Documento OpenAPI publico, requerido para que cualquier renderizador (Swagger UI, Scalar, etc.) y el boton Authorize funcionen sin autenticacion previa"));

// Habilitar middleware para servir swagger-ui (HTML, JS, CSS, etc.),
// especificando el endpoint JSON del documento OpenAPI nativo de arriba.
app.UseSwaggerUI(c =>
{
  c.SwaggerEndpoint("/openapi/v1.json", "AutoDiscoveryIAM API V1");
  c.RoutePrefix = "swagger";
});

// PRUEBA: renderizador alternativo apuntando al mismo documento OpenAPI nativo,
// sin tocar EndpointOperationDocs ni ningun endpoint -- demuestra en vivo que la herramienta de
// renderizado es intercambiable. Convive con Swagger UI (arriba) para poder comparar ambos; no
// reemplaza nada todavia.
app.MapScalarApiReference("/scalar", options => options.WithOpenApiRoutePattern("/openapi/{documentName}.json"))
  .WithMetadata(new PermissionsNotRequiredAttribute("Interfaz de documentacion publica (prueba de renderizador alternativo)"));

// Motor RBAC de AutoDiscoveryIAM: enrutamiento, manejo de excepciones, CORS, rate limiting,
// autenticación JWT, autorización por permisos, y el middleware transaccional de AutoDiscoveryIAMUnitOfWork -- ver
// Infrastructure/Composition/AutoDiscoveryIAMApplicationBuilderExtensions.cs.
app.UseAutoDiscoveryIAM();

// Mapear endpoints del motor RBAC de AutoDiscoveryIAM (health check y todos los endpoints de
// features vertical slice) -- ver Infrastructure/Composition/AutoDiscoveryIAMEndpointRouteBuilderExtensions.cs.
app.MapAutoDiscoveryIAMEndpoints();

// *******************************************
// 5. Inicialización de Base de Datos (con Lógica de Reintento)
// *******************************************
// Opcional, ver Infrastructure/DatabaseInitialization.cs -- espera a que la base de datos esté
// disponible (reintentos) y, solo en Development, aplica migraciones pendientes.
await app.InitializeDatabaseAsync();

// Seed de datos por defecto del motor RBAC: roles AdminAutoDiscoveryIAM/UserDefault, permisos esenciales
// y usuario de sistema. includeDevelopmentUsers: true agrega el admin de bootstrap y los usuarios de
// desarrollo (contraseñas públicas), y la librería lo ignora fuera de Development/Testing.
// Idempotente, seguro de correr en cada arranque.
await app.SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true, CancellationToken.None);

// *******************************************
// 6. Ejecutar la Aplicación
// *******************************************
app.Run();

public partial class Program { }
