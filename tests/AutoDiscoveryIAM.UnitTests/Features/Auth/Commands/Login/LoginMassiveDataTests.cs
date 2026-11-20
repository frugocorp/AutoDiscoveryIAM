// -----------------------------------------------------------------------
// <copyright file="LoginMassiveDataTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
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
using RefreshTokenEntity = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login;

/// <summary>
/// Tests masivos con datos externalizados para el sistema de login.
/// Valida comportamiento con grandes volúmenes de datos y casos diversos.
/// Estas pruebas se excluyen por defecto. Para ejecutarlas usar: dotnet test --filter "Category=Performance"
/// </summary>
[Trait("Category", "Performance")]
public class LoginMassiveDataTests : IDisposable
{
  private readonly ITestOutputHelper _output;
  private readonly AutoDiscoveryIAMDbContext _dbContext;
  private readonly LoginHandler _handler;
  private readonly PasswordHasher _passwordHasher;
  private readonly IAuthLogger _authLogger;
  private readonly IRefreshTokenService _refreshTokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository;

  public LoginMassiveDataTests(ITestOutputHelper output)
  {
    _output = output;

    // Configurar base de datos en memoria
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: $"LoginMassiveDataTestDb_{Guid.NewGuid()}")
      .Options;

    _dbContext = new AutoDiscoveryIAMDbContext(options);

    // Configurar servicios
    _passwordHasher = new PasswordHasher();
    _authLogger = Substitute.For<IAuthLogger>();
    _refreshTokenService = Substitute.For<IRefreshTokenService>();
    _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

    // Configure mock behavior
    _refreshTokenService.GenerateRefreshToken(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(new RefreshTokenEntity
      {
        Id = 1,
        Token = "mock-refresh-token-hash",
        PlaintextToken = "mock-refresh-token-12345",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        UserId = 1,
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
    // Add test users para massive data testing
    var users = new[]
    {
      new User
      {
        Id = 1,
        PublicId = Guid.NewGuid(),
        Email = "admin@example.com",
        Username = "admin",
        PasswordHash = _passwordHasher.HashPassword("AdminPass123!"),
        IsActive = true,
        IsEmailVerified = true,
        UserRoles = new List<UserRole>(),
        CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      },
      new User
      {
        Id = 2,
        PublicId = Guid.NewGuid(),
        Email = "user@example.com",
        Username = "user",
        PasswordHash = _passwordHasher.HashPassword("UserPass123!"),
        IsActive = true,
        IsEmailVerified = true,
        UserRoles = new List<UserRole>(),
        CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      },
      new User
      {
        Id = 3,
        PublicId = Guid.NewGuid(),
        Email = "manager@example.com",
        Username = "manager",
        PasswordHash = _passwordHasher.HashPassword("ManagerPass123!"),
        IsActive = true,
        IsEmailVerified = true,
        UserRoles = new List<UserRole>(),
        CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      },
      new User
      {
        Id = 4,
        PublicId = Guid.NewGuid(),
        Email = "test@example.com",
        Username = "test",
        PasswordHash = _passwordHasher.HashPassword("TestPass123!"),
        IsActive = true,
        IsEmailVerified = true,
        UserRoles = new List<UserRole>(),
        CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      },
    };

    _dbContext.Users.AddRange(users);
    _dbContext.SaveChanges();
  }

  /// <summary>
  /// Test masivo con casos válidos externalizados.
  /// Valida que todos los logins válidos funcionen correctamente.
  /// </summary>
  [Theory]
  [MemberData(nameof(LoginTestData.ValidLoginCases), MemberType = typeof(LoginTestData))]
  [Trait("Category", "MassiveData")]
  [Trait("Type", "ValidCredentials")]
  public async Task LoginHandler_MassiveValidCases_AllSucceed(string email, string password, string expectedHash, string testName)
  {
    // Arrange
    _output.WriteLine($"? Probando massive valid case: {testName}");
    var command = new LoginCommand(email, password, "127.0.0.1", $"Massive-Valid-{testName}");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    // For ValidLoginCases, all debe ser exitoso since they are valid credentials
    result.Success.Should().BeTrue($"Massive valid case {testName} debe ser exitoso");

    result.Data.Should().NotBeNull("Successful login debe tener data");
    result.Data!.Token.Should().NotBeNullOrEmpty("Successful login debe tener JWT token");
    result.Data.RefreshToken.Should().NotBeNullOrEmpty("Successful login debe tener refresh token");
    result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "El token debe expirar en el futuro");

    // Validate that the password matches the expected hash (additional verification)
    var passwordMatches = _passwordHasher.VerifyPassword(password, expectedHash);
    passwordMatches.Should().BeTrue($"La contraseña debe coincidir con el hash esperado para {testName}");

    _output.WriteLine($"   ? Resultado: Éxito={result.Success}, HasToken={result.Data?.Token != null}");
  }

  /// <summary>
  /// Test masivo con casos inválidos externalizados.
  /// Valida que todos los logins inválidos fallen correctamente.
  /// </summary>
  [Fact]
  [Trait("Category", "MassiveData")]
  [Trait("Type", "InvalidCredentials")]
  public async Task LoginHandler_MassiveInvalidCases_AllFail()
  {
    // Arrange
    var invalidCases = new[]
    {
      ("admin@example.com", "wrongpassword", "ValidUserWrongPassword"),
      ("nonexistent@example.com", "anypassword", "NonExistentUser"),
      ("fake@nowhere.com", "FakePass123!", "FakeUser"),
    };

    _output.WriteLine($"? Probando {invalidCases.Length} massive invalid cases...");

    // Act & Assert
    foreach (var (email, password, testName) in invalidCases)
    {
      var command = new LoginCommand(email, password, "127.0.0.1", $"Massive-Invalid-{testName}");
      var result = await _handler.Handle(command, CancellationToken.None);

      result.Success.Should().BeFalse($"El caso masivo inválido {testName} debe fallar");
      result.Error.Should().NotBeNullOrEmpty("Un inicio de sesión fallido debe incluir mensaje de error");
      result.ErrorInfo.Should().NotBeNull("Un inicio de sesión fallido debe incluir información de error");
      result.Data.Should().BeNull("Un inicio de sesión fallido no debe incluir datos");

      // Verificar mensaje genérico anti-enumeración
      result.Error.Should().Be(AuthValidationTypes.InvalidCredentials.Message,
        "El mensaje de error debe ser genérico para evitar enumeración de usuarios");

      _output.WriteLine($"   ? {testName}: Success={result.Success}, Error='{result.Error}'");
    }
  }

  /// <summary>
  /// Test masivo con casos de anti-enumeración.
  /// Valida que todos los casos devuelvan mensajes consistentes.
  /// </summary>
  [Theory]
  [MemberData(nameof(LoginTestData.AntiEnumerationCases), MemberType = typeof(LoginTestData))]
  [Trait("Category", "Security")]
  [Trait("Type", "AntiEnumeration")]
  public async Task LoginHandler_AntiEnumerationCases_ConsistentMessages(string email, string password, string expectedResult, string testName)
  {
    // Arrange
    _output.WriteLine($"??? Probando anti-enumeration case: {testName}");
    var command = new LoginCommand(email, password, "127.0.0.1", $"AntiEnum-{testName}");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    if (expectedResult == "success")
    {
      result.Success.Should().BeTrue($"Se esperaba éxito para {testName}");
      result.Data.Should().NotBeNull($"Success case {testName} debe tener data");
    }
    else
    {
      result.Success.Should().BeFalse($"Anti-enumeration case {testName} debe fallar");
      result.Error.Should().Be(AuthValidationTypes.InvalidCredentials.Message,
        $"El caso anti-enumeración {testName} debe tener mensaje de error genérico");
      result.ErrorInfo.Should().NotBeNull("Debe incluir información de error");
      result.Data.Should().BeNull("Un inicio de sesión fallido no debe incluir datos");
    }

    _output.WriteLine($"   ? Esperado: {expectedResult}, Obtenido: {(result.Success ? "éxito" : "fallo")}");
  }

  /// <summary>
  /// Test masivo con casos de carga distribuida.
  /// Procesa 1000+ casos de login para validar performance bajo carga.
  /// </summary>
  [Fact]
  [Trait("Category", "MassiveData")]
  [Trait("Type", "LoadTest")]
  public async Task LoginHandler_MassiveLoadTest_HandlesHighVolume()
  {
    // Arrange
    _output.WriteLine("?? Iniciando prueba masiva de carga con 1000+ casos...");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var totalCases = 0;
    var successCount = 0;
    var failureCount = 0;

    // Act - Procesar todos los casos masivos
    foreach (var testCase in LoginTestData.MassiveLoginCases())
    {
      var email = (string)testCase[0];
      var password = (string)testCase[1];
      var expectedResult = (string)testCase[2];
      var testName = (string)testCase[3];

      var command = new LoginCommand(email, password, "127.0.0.1", $"Load-{testName}");
      var result = await _handler.Handle(command, CancellationToken.None);

      var shouldSucceed = expectedResult == "success";
      result.Success.Should().Be(shouldSucceed, $"El caso de carga {testName} debe coincidir con el resultado esperado");

      totalCases++;
      if (result.Success)
        successCount++;
      else
        failureCount++;

      // Progress logging cada 200 casos
      if (totalCases % 200 == 0)
      {
        _output.WriteLine($"   ?? Progress: {totalCases} cases processed in {stopwatch.ElapsedMilliseconds}ms");
      }
    }

    stopwatch.Stop();

    // Assert - Validaciones de carga masiva
    totalCases.Should().BeGreaterThanOrEqualTo(1000, "Debe procesar 1000+ casos");
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1200000,
      $"La prueba masiva de carga debe completarse en menos de 20 minutos: {stopwatch.ElapsedMilliseconds}ms (Argon2id m=65536,t=3,p=1 es memory-hard e intencionalmente ~2-3x más lento que BCrypt WF12 por operación, considerando hardware de desarrollo)");

    var averageTimePerCase = (double)stopwatch.ElapsedMilliseconds / totalCases;
    averageTimePerCase.Should().BeLessThan(1200,
      $"El tiempo promedio por caso debe ser razonable: {averageTimePerCase:F2}ms (Argon2id por defecto toma ~500-850ms por hash/verify en hardware de desarrollo)");

    // Log estadísticas finales
    _output.WriteLine("?? RESULTADOS DE PRUEBA MASIVA DE CARGA:");
    _output.WriteLine($"   Total cases: {totalCases}");
    _output.WriteLine($"   Successful logins: {successCount}");
    _output.WriteLine($"   Inicios de sesión fallidos: {failureCount}");
    _output.WriteLine($"   Total time: {stopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"   Tiempo promedio por caso: {averageTimePerCase:F2}ms");
    _output.WriteLine($"   Throughput: {totalCases * 1000.0 / stopwatch.ElapsedMilliseconds:F1} cases/second");
  }

  /// <summary>
  /// Test de validación de formato con datos externalizados.
  /// </summary>
  [Theory]
  [MemberData(nameof(LoginTestData.FormatValidationCases), MemberType = typeof(LoginTestData))]
  [Trait("Category", "MassiveData")]
  [Trait("Type", "FormatValidation")]
  public async Task LoginHandler_FormatValidationCases_HandleGracefully(string email, string password, string expectedResult, string testName)
  {
    // Arrange
    _output.WriteLine($"?? Probando format validation: {testName}");
    var command = new LoginCommand(email, password, "127.0.0.1", $"Format-{testName}");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    var shouldSucceed = expectedResult == "success";
    result.Success.Should().Be(shouldSucceed, $"El caso de validación de formato {testName} debe coincidir con el resultado esperado");

    // El sistema no debe fallar con inputs mal formateados
    result.Should().NotBeNull("El handler siempre debe devolver un resultado y nunca lanzar excepción");

    if (!shouldSucceed)
    {
      result.Error.Should().NotBeNullOrEmpty("Un formato inválido debe tener mensaje de error");
    }

    _output.WriteLine($"   ? Formato procesado: Éxito={result.Success}, CorreoNulo={email == null}, ContraseñaNula={password == null}");
  }

  public void Dispose()
  {
    _dbContext?.Dispose();
    GC.SuppressFinalize(this);
  }
}



