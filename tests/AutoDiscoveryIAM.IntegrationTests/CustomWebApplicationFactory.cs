// -----------------------------------------------------------------------
// <copyright file="CustomWebApplicationFactory.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests
{
  /// <summary>
  /// Fábrica personalizada para pruebas de integración.
  /// Permite alternar entre una base de datos real y una en memoria según la variable de entorno 'USE_REAL_DB_FOR_TESTS'.
  /// Utiliza comparación insensible a mayúsculas/minúsculas con <see cref="StringComparison.OrdinalIgnoreCase"/> para evitar errores culturales.
  /// - Si 'USE_REAL_DB_FOR_TESTS' es "true" (sin importar mayúsculas/minúsculas), se usa SQL Server y la cadena de conexión de 'CONNECTIONSTRINGS_DEFAULTCONNECTION'.
  /// - En cualquier otro caso, se usa una base de datos en memoria.
  /// Asegura la creación de la base de datos antes de iniciar las pruebas.
  /// </summary>
  /// <typeparam name="T">
  /// Tipo de clase de inicio (Startup) de la aplicación ASP.NET Core bajo prueba.
  /// Debe ser una clase pública que configure los servicios y el pipeline de la aplicación.
  /// </typeparam>
  /// <remarks>
  /// Es importante utilizar <c>string.Equals(..., StringComparison.OrdinalIgnoreCase)</c> para comparar valores de variables de entorno,
  /// ya que <c>ToLower()</c> o <c>ToUpper()</c> pueden generar resultados inconsistentes en entornos con diferentes configuraciones regionales.
  /// </remarks>
  public class CustomWebApplicationFactory<T> : WebApplicationFactory<T>
  where T : class
  {
    const bool consoleWriteLine = false;

    /// <summary>
    /// Si es <c>false</c> (el caso general) no se aplica ningún límite de peticiones: en el TestServer
    /// todas las peticiones comparten la misma "IP", y muchas clases hacen login decenas de veces por
    /// minuto. Las pruebas de rate limiting usan <see cref="RateLimitedWebApplicationFactory"/>, que
    /// conserva exactamente los límites por defecto de la librería
    /// (<see cref="AutoDiscoveryIAMRateLimitDefaults"/>), sin las reglas generales del appsettings de
    /// SampleHost, para probarlos tal como los recibe un consumidor.
    /// </summary>
    protected virtual bool EnforceLibraryRateLimits => false;
    /// <summary>
    /// Configura el host web para pruebas, permitiendo seleccionar entre una base de datos real o en memoria.
    /// Utiliza comparación de cadenas insensible a mayúsculas/minúsculas con StringComparison.OrdinalIgnoreCase
    /// para garantizar un comportamiento consistente y seguro al leer variables de entorno.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      builder.ConfigureAppConfiguration((_, config) =>
      {
        config.AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false);

        var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
        if (File.Exists(testSettingsPath))
        {
          config.AddJsonFile(testSettingsPath, optional: false, reloadOnChange: false);
        }
      });

      builder.ConfigureServices(services =>
      {
        services.PostConfigure<RateLimitingSettings>(options => options.Rules = EnforceLibraryRateLimits
          ? options.Rules.Where(rule => AutoDiscoveryIAMRateLimitDefaults.Rules.Any(d => d.Endpoint == rule.Endpoint && d.Period == rule.Period)).ToList()
          : []);

        services.RemoveAll<IActivationEmailService>();
        services.AddSingleton<IActivationEmailService, FakeActivationEmailService>();

        // Remover la configuración existente del DbContext
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AutoDiscoveryIAMDbContext>));

        if (descriptor != null)
        {
          services.Remove(descriptor);
        }

        ConfigureDatabase(services);
        EnsureDatabaseCreatedAndSeeded(services);
      });
    }

    private sealed class FakeActivationEmailService : IActivationEmailService
    {
      public Task<OperationResult<bool>> SendActivationEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
        => Task.FromResult(OperationResult.Ok(true));

      public Task<OperationResult<bool>> SendAccountActivatedEmailAsync(User user, CancellationToken cancellationToken, bool fakeMail = false)
        => Task.FromResult(OperationResult.Ok(true));

      public Task<OperationResult<bool>> SendRegistrationAttemptNoticeAsync(string email, CancellationToken cancellationToken, bool fakeMail = false)
        => Task.FromResult(OperationResult.Ok(true));

      public Task<OperationResult<bool>> SendTestEmailAsync(string? subject, string? body, CancellationToken cancellationToken, bool fakeMail = false)
        => Task.FromResult(OperationResult.Ok(true));
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && IsUsingRealDatabase())
      {
        try
        {
          using var scope = Services.CreateScope();
          var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
          CleanUpDbAfterTests.CleanupTestData(db);
        }
        catch
        {
          // No interrumpir la finalización del test runner por cleanup best-effort.
        }
      }

      base.Dispose(disposing);
    }

    /// <summary>
    /// Configures the database para tests, allowing real database or in-memory selection.
    /// </summary>
    private static void ConfigureDatabase(IServiceCollection services)
    {
      // Leer variable de entorno para decidir el tipo de DB
      if (consoleWriteLine) Console.WriteLine($"********* CustomWebApplicationFactory -- USE_REAL_DB_FOR_TESTS: {Environment.GetEnvironmentVariable("USE_REAL_DB_FOR_TESTS")}");
      if (IsUsingRealDatabase())
      {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_DEFAULTCONNECTION");
        if (consoleWriteLine) Console.WriteLine($"********* CustomWebApplicationFactory -- CONNECTIONSTRINGS_DEFAULTCONNECTION: {connectionString}");
        if (string.IsNullOrEmpty(connectionString))
        {
          throw new InvalidOperationException("No se encontró la cadena de conexión CONNECTIONSTRINGS_DEFAULTCONNECTION en el entorno.");
        }

        services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options.UseSqlServer(connectionString));
        if (consoleWriteLine) Console.WriteLine($"********* CustomWebApplicationFactory -- DB: SQL Server");
      }
      else
      {
        services.AddDbContext<AutoDiscoveryIAMDbContext>(options =>
        {
          options.UseInMemoryDatabase("InMemoryDbForProbando");
          // Suprimir warning de transacciones no soportadas en InMemory
          // Event ID 20401: InMemoryEventId.TransactionIgnoredWarning
          options.ConfigureWarnings(x => x.Ignore(new Microsoft.Extensions.Logging.EventId(20401)));
        });
        if (consoleWriteLine) Console.WriteLine($"********* CustomWebApplicationFactory -- DB: InMemory");
      }
    }

    /// <summary>
    /// Ensures database is created and seeded with test data.
    /// </summary>
    private static void EnsureDatabaseCreatedAndSeeded(IServiceCollection services)
    {
      // Crear el servicio y asegurarnos de que la base de datos está creada
      var sp = services.BuildServiceProvider();
      using var scope = sp.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var db = scopedServices.GetRequiredService<AutoDiscoveryIAMDbContext>();
      if (consoleWriteLine) Console.WriteLine($"********* ProviderName: {db.Database.ProviderName}");
      if (consoleWriteLine) Console.WriteLine($"********* CustomWebApplicationFactory -- DB: EnsureCreated");
      db.Database.EnsureCreated();

      if (IsUsingRealDatabase())
      {
        CleanUpDbAfterTests.CleanupTestData(db);
      }

      // Seed test users if database is empty
      SeedTestUsers(db);
    }

    private static bool IsUsingRealDatabase()
    {
      var useRealDb = Environment.GetEnvironmentVariable("USE_REAL_DB_FOR_TESTS");
      return !string.IsNullOrEmpty(useRealDb) && string.Equals(useRealDb, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Seeds test users into the database para integration tests.
    /// </summary>
    private static void SeedTestUsers(AutoDiscoveryIAMDbContext db)
    {
      // Check if users already exist to avoid duplicates
      if (db.Users.Any())
      {
        if (consoleWriteLine) Console.WriteLine($"********* CustomWebApplicationFactory -- Users already seeded");
        return;
      }

      var testUsers = CreateTestUsers();
      db.Users.AddRange(testUsers);
      db.SaveChanges();
      if (consoleWriteLine) Console.WriteLine($"********* CustomWebApplicationFactory -- Test users seeded");
    }

    /// <summary>
    /// Creates the test users para integration tests.
    /// </summary>
    /// <remarks>
    /// Credenciales (username/email/hash) tomadas de <see cref="UserSeeder.AdminBootstrapUser"/> y
    /// <see cref="DevUserSeeder.DevUsers"/> (vía <c>InternalsVisibleTo</c>) -- única fuente de verdad,
    /// para no duplicar los mismos hashes Argon2id en el proyecto de tests. Solo el <c>PublicId</c>
    /// fijo es propio de este factory (conveniencia para referenciar el usuario en tests por Guid
    /// conocido).
    /// </remarks>
    private static FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User[] CreateTestUsers()
    {
      return new[]
      {
        CreateAdminUser(),
        CreateRegularUser(),
        CreateDevelopmentUser(),
        CreateSuAdminUser(),
      };
    }

    private static FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User CreateAdminUser()
    {
      var admin = UserSeeder.AdminBootstrapUser;
      var user = BuildTestUser(admin.Username, admin.Email, admin.PasswordHash, Guid.Parse("550e8400-e29b-41d4-a716-446655440001"));

      // Mismo flag que le pone UserSeeder: sobre una base vacía este factory siembra primero, y el
      // seeder de la app ya no lo crea (existe por email).
      user.IsAdmin = true;
      return user;
    }

    private static FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User CreateRegularUser()
      => CreateFromDevUserSeed("user", Guid.Parse("550e8400-e29b-41d4-a716-446655440002"));

    private static FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User CreateDevelopmentUser()
      => CreateFromDevUserSeed("dev", Guid.Parse("550e8400-e29b-41d4-a716-446655440003"));

    private static FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User CreateSuAdminUser()
      => CreateFromDevUserSeed("su", Guid.Parse("550e8400-e29b-41d4-a716-446655440004"));

    private static FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User CreateFromDevUserSeed(string username, Guid publicId)
    {
      var devUser = DevUserSeeder.DevUsers.First(u => u.Username == username);
      var user = BuildTestUser(devUser.Username, devUser.Email, devUser.PasswordHash, publicId);

      // Mismos flags que les pone DevUserSeeder (ver CreateAdminUser).
      user.IsDevelopment = devUser.IsDevelopment;
      user.IsSuAdmin = devUser.IsSuAdmin;
      return user;
    }

    private static FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User BuildTestUser(
      string username, string email, string passwordHash, Guid publicId)
    {
      return new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User
      {
        Username = username,
        Email = email,
        PasswordHash = passwordHash,
        Status = "active",
        IsActive = true,
        PublicId = publicId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = 1L,
        UpdatedBy = 1L,
        IsDeleted = false,
      };
    }
  }
}
