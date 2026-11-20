// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMSeedingExtensions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Upgrades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Punto de entrada público del seed de datos por defecto del motor RBAC -- invocable directamente,
/// sin depender de que las migraciones se distribuyan, para que un proyecto que instale el paquete
/// NuGet obtenga sus datos iniciales (roles <see cref="RoleSeeder.AdminAutoDiscoveryIamRoleName"/> y
/// <see cref="RoleSeeder.UserDefaultRoleName"/>, permisos esenciales y usuario de sistema; el usuario
/// admin de bootstrap y los usuarios de desarrollo solo con opt-in explícito y en Development/Testing).
/// </summary>
public static partial class AutoDiscoveryIAMSeedingExtensions
{
  private const int MaxAttempts = 3;

  /// <summary>
  /// Serializa las siembras concurrentes dentro del mismo proceso (ej. las 56 clases de prueba de
  /// integración, cada una con su propio <c>WebApplicationFactory</c>, que xUnit arranca en
  /// paralelo por defecto). El reintento sobre <see cref="DbUpdateException"/> por sí solo no
  /// alcanza a absorber esa cantidad de arranques simultáneos contra la misma base de datos.
  /// </summary>
  private static readonly SemaphoreSlim SeedLock = new(1, 1);

  /// <summary>
  /// Aplica el seed de datos por defecto del motor RBAC, si hace falta, sin las cuentas de
  /// desarrollo con contraseña conocida. Idempotente -- seguro de llamar en cada arranque de la app,
  /// incluyendo varias instancias arrancando en paralelo contra la misma base de datos (ej. réplicas
  /// de Kubernetes, o varias <c>WebApplicationFactory</c> de pruebas de integración concurrentes).
  /// </summary>
  /// <remarks>
  /// Cada seeder individual revisa existencia antes de insertar, pero eso no es atómico entre
  /// instancias concurrentes: dos procesos pueden revisar "no existe" al mismo tiempo y ambos
  /// intentar insertar, y uno de los dos choca contra una restricción única. Dentro del mismo
  /// proceso, <see cref="SeedLock"/> serializa por completo el acceso -- la primera llamada siembra,
  /// el resto ve el chequeo de existencia ya satisfecho y retorna de inmediato. Entre procesos
  /// distintos (ej. réplicas), el lock en memoria no ayuda, así que además se reintenta sobre
  /// <see cref="DbUpdateException"/>: en el reintento, los chequeos de existencia ya ven las filas
  /// que la otra instancia alcanzó a confirmar primero. No se usa un lock distribuido atado a un
  /// proveedor específico (ej. <c>sp_getapplock</c> de SQL Server) para mantener el motor agnóstico
  /// de proveedor.
  /// </remarks>
  public static Task SeedAutoDiscoveryIAMDefaultDataAsync(this WebApplication app, CancellationToken cancellationToken = default)
    => app.SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: false, cancellationToken);

  /// <summary>
  /// Igual que <see cref="SeedAutoDiscoveryIAMDefaultDataAsync(WebApplication, CancellationToken)"/>,
  /// pero con <paramref name="includeDevelopmentUsers"/> en <c>true</c> siembra además el admin de
  /// bootstrap y los usuarios de desarrollo (contraseñas conocidas, publicadas en el README),
  /// siempre que el ambiente sea Development o Testing. En cualquier otro ambiente se ignora.
  /// </summary>
  /// <remarks>
  /// Fuera de Development/Testing, si esas cuentas existen y están activas en la base de datos
  /// (por ejemplo, porque se copió una base de desarrollo), se escribe un log crítico al arrancar.
  /// </remarks>
  public static async Task SeedAutoDiscoveryIAMDefaultDataAsync(this WebApplication app, bool includeDevelopmentUsers, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AutoDiscoveryIAMSeedingExtensions));

    await SeedLock.WaitAsync(cancellationToken);
    try
    {
      await SeedWithRetryAsync(dbContext, environment.EnvironmentName, includeDevelopmentUsers, cancellationToken);
    }
    finally
    {
      SeedLock.Release();
    }

    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
      return;
    }

    if (includeDevelopmentUsers)
    {
      LogDevelopmentUsersIgnored(logger, environment.EnvironmentName);
    }

    var knownPasswordAccounts = await RbacSeeder.FindActiveKnownPasswordAccountsAsync(dbContext, cancellationToken);
    if (knownPasswordAccounts.Count > 0)
    {
      var accounts = string.Join(", ", knownPasswordAccounts);
      LogKnownPasswordAccountsActive(logger, environment.EnvironmentName, accounts);
    }
  }

  private static async Task SeedWithRetryAsync(AutoDiscoveryIAMDbContext dbContext, string environmentName, bool includeDevelopmentUsers, CancellationToken cancellationToken)
  {
    for (var attempt = 1; attempt <= MaxAttempts; attempt++)
    {
      try
      {
        // Primero los cambios de datos de una versión nueva del paquete (ej. renombrar una clave
        // conservando sus asignaciones), después el seed: así el seed nunca recrea una clave vieja.
        await AutoDiscoveryIAMUpgradeRunner.RunAsync(dbContext, AutoDiscoveryIAMUpgradeSteps.All, cancellationToken);
        await RbacSeeder.SeedAllAsync(dbContext, environmentName, includeDevelopmentUsers, cancellationToken);
        return;
      }
      catch (DbUpdateException) when (attempt < MaxAttempts)
      {
        dbContext.ChangeTracker.Clear();
        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
      }
    }
  }

  [LoggerMessage(
    EventId = 9001,
    Level = LogLevel.Warning,
    Message = "AutoDiscoveryIAM: includeDevelopmentUsers se ignora en el ambiente {EnvironmentName}; los usuarios de desarrollo solo se siembran en Development o Testing.")]
  private static partial void LogDevelopmentUsersIgnored(ILogger logger, string environmentName);

  [LoggerMessage(
    EventId = 9002,
    Level = LogLevel.Critical,
    Message = "AutoDiscoveryIAM: el ambiente {EnvironmentName} tiene activas cuentas de desarrollo con contraseña pública ({Accounts}). Elimínalas o desactívalas: cualquiera puede iniciar sesión con ellas.")]
  private static partial void LogKnownPasswordAccountsActive(ILogger logger, string environmentName, string accounts);
}
