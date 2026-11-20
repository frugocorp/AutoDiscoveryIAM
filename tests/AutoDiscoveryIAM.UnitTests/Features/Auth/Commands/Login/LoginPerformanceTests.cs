// -----------------------------------------------------------------------
// <copyright file="LoginPerformanceTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.Data;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login;

/// <summary>
/// Tests de performance para el sistema de login.
/// Valida que el sistema mantenga rendimiento bajo carga de 1000+ combinaciones.
/// Estas pruebas se excluyen por defecto. Para ejecutarlas usar: dotnet test --filter "Category=Performance"
/// </summary>
[Trait("Category", "Performance")]
public class LoginPerformanceTests : IDisposable
{
  private readonly ITestOutputHelper _output;
  private readonly AutoDiscoveryIAMDbContext _dbContext;
  private readonly LoginHandler _handler;
  private readonly PasswordHasher _passwordHasher;
  private readonly IAuthLogger _authLogger;
  private readonly IRefreshTokenService _refreshTokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository;

  public LoginPerformanceTests(ITestOutputHelper output)
  {
    _output = output;

    // Configurar base de datos en memoria
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: $"LoginPerformanceTestDb_{Guid.NewGuid()}")
      .Options;

    _dbContext = new AutoDiscoveryIAMDbContext(options);

    // Configurar servicios
    _passwordHasher = new PasswordHasher();
    _authLogger = Substitute.For<IAuthLogger>();
    _refreshTokenService = Substitute.For<IRefreshTokenService>();
    _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

    // Configurar mock del refresh token service (basado en LoginHandlerTests.cs)
    _refreshTokenService.GenerateRefreshToken(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(callInfo => new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
      {
        Token = "mock-refresh-token-12345",
        UserId = callInfo.Arg<User>().Id,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IpAddress = callInfo.ArgAt<string>(1),
        UserAgent = callInfo.ArgAt<string>(2),
        IsRevoked = false,
      });

    // Configurar opciones JWT
    var jwtSettings = new JwtSettings
    {
      Key = "test-key-that-is-long-enough-for-jwt-signing-requirements-minimum-256-bits",
      Issuer = "test-issuer",
      Audience = "test-audience",
      ExpiryMinutes = 60,
    };
    var jwtOptions = Options.Create(jwtSettings);
    var jwtClaimsFactory = new JwtClaimsFactory(jwtOptions);
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var lockoutSettings = Options.Create(new LockoutSettings());

    _handler = new LoginHandler(
      _authLogger, _dbContext, _passwordHasher, _refreshTokenService, _refreshTokenRepository,
      jwtClaimsFactory, unitOfWork, lockoutSettings,
      Substitute.For<ITwoFactorCodeGenerator>(), Substitute.For<ITwoFactorEmailService>(), Options.Create(new TwoFactorSettings()),
      Substitute.For<ISecurityEventLogger>());

    // Sembrar datos de prueba
    SeedTestData();
  }

  private void SeedTestData()
  {
    // Add test users para performance testing con todas las propiedades necesarias
    var adminUser = new User
    {
      Id = 1,
      PublicId = Guid.NewGuid(),
      Email = "admin@example.com",
      Username = "admin",
      PasswordHash = _passwordHasher.HashPassword("AdminPass123!"),
      IsActive = true,
      IsEmailVerified = true,
      CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      UserRoles = new List<UserRole>(), // Inicializar collection vacía
    };

    var regularUser = new User
    {
      Id = 2,
      PublicId = Guid.NewGuid(),
      Email = "user@example.com",
      Username = "user",
      PasswordHash = _passwordHasher.HashPassword("UserPass123!"),
      IsActive = true,
      IsEmailVerified = true,
      CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      UserRoles = new List<UserRole>(), // Inicializar collection vacía
    };

    _dbContext.Users.AddRange(adminUser, regularUser);
    _dbContext.SaveChanges();
  }

  /// <summary>
  /// Crea un nuevo LoginHandler con su propio DbContext para concurrencia.
  /// </summary>
  private LoginHandler CreateConcurrentHandler()
  {
    // Crear nueva instancia de DbContext para concurrencia
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: $"LoginPerformanceTestDb_{Guid.NewGuid()}")
      .Options;

    var dbContext = new AutoDiscoveryIAMDbContext(options);

    // Recrear los mismos usuarios en la nueva instancia
    var adminUser = new User
    {
      Id = 1,
      PublicId = Guid.NewGuid(),
      Email = "admin@example.com",
      Username = "admin",
      PasswordHash = _passwordHasher.HashPassword("AdminPass123!"),
      IsActive = true,
      IsEmailVerified = true,
      CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      UserRoles = new List<UserRole>(),
    };

    var regularUser = new User
    {
      Id = 2,
      PublicId = Guid.NewGuid(),
      Email = "user@example.com",
      Username = "user",
      PasswordHash = _passwordHasher.HashPassword("UserPass123!"),
      IsActive = true,
      IsEmailVerified = true,
      CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      UserRoles = new List<UserRole>(),
    };

    dbContext.Users.AddRange(adminUser, regularUser);
    dbContext.SaveChanges();

    // Crear mocks para este handler
    var authLogger = Substitute.For<IAuthLogger>();
    var refreshTokenService = Substitute.For<IRefreshTokenService>();
    var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

    refreshTokenService.GenerateRefreshToken(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(callInfo => new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
      {
        Token = "mock-refresh-token-12345",
        UserId = callInfo.Arg<User>().Id,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IpAddress = callInfo.ArgAt<string>(1),
        UserAgent = callInfo.ArgAt<string>(2),
        IsRevoked = false,
      });

    var jwtSettings = new JwtSettings
    {
      Key = "test-key-that-is-long-enough-for-jwt-signing-requirements-minimum-256-bits",
      Issuer = "test-issuer",
      Audience = "test-audience",
      ExpiryMinutes = 60,
    };
    var jwtOptions = Options.Create(jwtSettings);
    var jwtClaimsFactory = new JwtClaimsFactory(jwtOptions);
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var lockoutSettings = Options.Create(new LockoutSettings());

    return new LoginHandler(
      authLogger, dbContext, _passwordHasher, refreshTokenService, refreshTokenRepository,
      jwtClaimsFactory, unitOfWork, lockoutSettings,
      Substitute.For<ITwoFactorCodeGenerator>(), Substitute.For<ITwoFactorEmailService>(), Options.Create(new TwoFactorSettings()),
      Substitute.For<ISecurityEventLogger>());
  }

  public void Dispose()
  {
    _dbContext?.Dispose();
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Test de performance con 1000+ combinaciones de credenciales.
  /// Valida que el sistema procese gran volumen de requests en tiempo razonable.
  /// </summary>
  [Fact]
  [Trait("Category", "Performance")]
  [Trait("Type", "LongRunning")]
  public async Task LoginHandler_With1000PlusCombinations_CompletesInReasonableTime()
  {
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    var totalCases = 0;
    var successfulLogins = 0;
    var failedLogins = 0;
    const int maxAcceptableTimeMs = 1200000; // 20 minutos máximo para 1000+ casos con Argon2id (memory-hard, ~2-3x más lento que BCrypt WF12 por operación)

    _output.WriteLine("?? Iniciando test de performance con 1000+ combinaciones...");

    // Act & Assert - Procesar todos los casos de performance
    foreach (var testCase in LoginPerformanceDataGenerator.PerformanceLoginCases)
    {
      var email = (string)testCase[0];
      var password = (string)testCase[1];
      var expectedSuccess = (bool)testCase[2];
      var testName = (string)testCase[3];

      var command = new LoginCommand(email, password, "127.0.0.1", "Performance-Test-Agent");

      var caseStopwatch = Stopwatch.StartNew();
      var result = await _handler.Handle(command, CancellationToken.None);
      caseStopwatch.Stop();

      // Validar resultado esperado
      result.Success.Should().Be(expectedSuccess,
        $"El caso de prueba {testName} debe tener éxito = {expectedSuccess}");

      // Validar tiempo por caso individual (Argon2id m=65536,t=3,p=1 es intencionalmente memory-hard; bajo ci-full con cobertura y ejecución paralela la contención puede llevarlo a 6-7s+)
      caseStopwatch.ElapsedMilliseconds.Should().BeLessThan(15000,
        $"Individual login case {testName} took too long: {caseStopwatch.ElapsedMilliseconds}ms (consider hardware capabilities)");

      totalCases++;
      if (result.Success)
        successfulLogins++;
      else
        failedLogins++;

      // Log progreso cada 100 casos
      if (totalCases % 100 == 0)
      {
        _output.WriteLine($"? Procesados {totalCases} casos en {stopwatch.ElapsedMilliseconds}ms");
      }
    }

    stopwatch.Stop();

    // Assert - Validaciones finales de performance
    totalCases.Should().BeGreaterThan(1000, "Debe probar más de 1000 combinaciones");
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxAcceptableTimeMs,
      $"Total processing time exceeded acceptable limit: {stopwatch.ElapsedMilliseconds}ms > {maxAcceptableTimeMs}ms");

    var averageTimePerCase = (double)stopwatch.ElapsedMilliseconds / totalCases;
    averageTimePerCase.Should().BeLessThan(1200,
      $"El tiempo promedio por caso es demasiado alto: {averageTimePerCase:F2}ms (esperado <1200ms con Argon2id en hardware de desarrollo)");

    // Log estadísticas finales
    _output.WriteLine("?? ESTADÍSTICAS FINALES DE PERFORMANCE:");
    _output.WriteLine($"   Total casos procesados: {totalCases}");
    _output.WriteLine($"   Logins exitosos: {successfulLogins}");
    _output.WriteLine($"   Inicios de sesión fallidos: {failedLogins}");
    _output.WriteLine($"   Tiempo total: {stopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"   Tiempo promedio por caso: {averageTimePerCase:F2}ms");
    _output.WriteLine($"   Throughput: {totalCases * 1000.0 / stopwatch.ElapsedMilliseconds:F1} casos/segundo");
  }

  /// <summary>
  /// Test de concurrencia simulando múltiples usuarios logueándose simultáneamente.
  /// </summary>
  [Fact]
  [Trait("Category", "Performance")]
  [Trait("Type", "Concurrency")]
  public async Task LoginHandler_ConcurrentRequests_HandlesLoadCorrectly()
  {
    // Arrange
    var concurrentTasks = new List<Task<bool>>();
    var stopwatch = Stopwatch.StartNew();
    const int concurrencyLevel = 50; // 50 requests simultáneos

    _output.WriteLine($"?? Iniciando test de concurrencia con {concurrencyLevel} requests simultáneos...");

    // Act - Ejecutar requests concurrentes
    foreach (var testCase in LoginPerformanceDataGenerator.ConcurrentLoginCases.Take(concurrencyLevel))
    {
      var threadId = (int)testCase[0];
      var email = (string)testCase[1];
      var password = (string)testCase[2];
      var expectedSuccess = (bool)testCase[3];

      var task = Task.Run(async () =>
      {
        var concurrentHandler = CreateConcurrentHandler();
        var command = new LoginCommand(email, password, "127.0.0.1", $"Concurrent-Test-{threadId}");
        var result = await concurrentHandler.Handle(command, CancellationToken.None);
        return result.Success == expectedSuccess;
      });

      concurrentTasks.Add(task);
    }

    var results = await Task.WhenAll(concurrentTasks);
    stopwatch.Stop();

    // Assert
    results.Should().NotContainNulls("Todas las tareas concurrentes deben completarse");
    results.All(s => s).Should().BeTrue("Todas las validaciones deben pasar");

    stopwatch.ElapsedMilliseconds.Should().BeLessThan(120000,
      $"Concurrent execution took too long: {stopwatch.ElapsedMilliseconds}ms (Argon2id is memory-hard, consider development hardware; margen ampliado por ruido de ci-full con coverage)");

    _output.WriteLine($"? {concurrencyLevel} requests concurrentes completados en {stopwatch.ElapsedMilliseconds}ms");
  }

  /// <summary>
  /// Test de stress con Argon2id a través de varias corridas por password.
  /// Valida que el hashing no se vuelva prohibitivamente lento.
  /// </summary>
  [Fact]
  [Trait("Category", "Performance")]
  [Trait("Type", "Argon2idStress")]
  public void PasswordHasher_Argon2idPerformance_MaintainsAcceptableSpeed()
  {
    // Arrange
    var results = new List<(int Iteration, string Password, long TimeMs)>();

    _output.WriteLine("?? Iniciando test de performance de Argon2id...");

    // Act - Test de varias corridas y tipos de password
    foreach (var testCase in LoginPerformanceDataGenerator.PasswordHasherPerformanceCases.Take(20))
    {
      var iteration = (int)testCase[0];
      var password = (string)testCase[1];
      var testName = (string)testCase[2];

      var stopwatch = Stopwatch.StartNew();

      var hash = _passwordHasher.HashPassword(password);
      var isValid = _passwordHasher.VerifyPassword(password, hash);

      stopwatch.Stop();

      isValid.Should().BeTrue($"Password verification debe ser exitoso para {testName}");

      results.Add((iteration, password, stopwatch.ElapsedMilliseconds));

      _output.WriteLine($"   Run{iteration} - {password.Length} chars: {stopwatch.ElapsedMilliseconds}ms");
    }

    // Assert - Ninguna corrida debe exceder 20000ms por operación (hash + verify, Argon2id m=65536,t=3,p=1)
    // -- margen ampliado para absorber ci-full (cobertura + ejecucion paralela) en hardware modesto
    // (medido hasta ~5.9s por operacion individual en un i7-7700HQ de 4 nucleos bajo ci-full completo).
    results.Should().NotBeEmpty("Debe haber pruebas de performance de Argon2id");
    results.All(r => r.TimeMs < 20000).Should().BeTrue($"Ninguna operación debe exceder 20000ms (promedio: {results.Average(r => r.TimeMs):F1}ms, considerar hardware de desarrollo bajo ci-full)");

    _output.WriteLine($"? Prueba de rendimiento Argon2id completada. Promedio: {results.Average(r => r.TimeMs):F1}ms");
  }
}



