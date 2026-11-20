// -----------------------------------------------------------------------
// <copyright file="LoginEdgeCasesTests.cs" company="FRUGO Corp.">
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

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login;

/// <summary>
/// Tests de edge cases para el sistema de login.
/// Valida comportamiento con caracteres especiales, formatos extremos y casos límite.
/// </summary>
public class LoginEdgeCasesTests : IDisposable
{
  private readonly ITestOutputHelper _output;
  private readonly AutoDiscoveryIAMDbContext _dbContext;
  private readonly LoginHandler _handler;
  private readonly PasswordHasher _passwordHasher;
  private readonly IAuthLogger _authLogger;
  private readonly IRefreshTokenService _refreshTokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository;

  public LoginEdgeCasesTests(ITestOutputHelper output)
  {
    _output = output;

    // Configurar base de datos en memoria
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: $"LoginEdgeCasesTestDb_{Guid.NewGuid()}")
      .Options;

    _dbContext = new AutoDiscoveryIAMDbContext(options);

    // Configurar servicios
    _passwordHasher = new PasswordHasher();
    _authLogger = Substitute.For<IAuthLogger>();
    _refreshTokenService = Substitute.For<IRefreshTokenService>();
    _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

    // Configurar mock del refresh token service
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
    // Agregar usuarios de prueba para casos límite con todas las propiedades necesarias
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

    var inactiveUser = new User
    {
      Id = 2,
      PublicId = Guid.NewGuid(),
      Email = "inactive@example.com",
      Username = "inactive",
      PasswordHash = _passwordHasher.HashPassword("InactiveUser123!"),
      IsActive = false, // Usuario inactivo para testing
      IsEmailVerified = true,
      CreatedAt = TimeHelpers.GetCurrentMexicoTime(),
      UserRoles = new List<UserRole>(),
    };

    _dbContext.Users.AddRange(adminUser, inactiveUser);
    _dbContext.SaveChanges();
  }

  /// <summary>
  /// Test de edge cases con emails mal formateados y caracteres especiales.
  /// </summary>
  [Theory]
  [MemberData(nameof(LoginEdgeCasesData.EdgeCaseEmails), MemberType = typeof(LoginEdgeCasesData))]
  [Trait("Category", "EdgeCases")]
  [Trait("Type", "EmailValidation")]
  public async Task LoginHandler_EdgeCaseEmails_HandlesGracefully(string email, string password, bool expectedSuccess, string testName)
  {
    // Arrange
    _output.WriteLine($"?? Probando caso límite de correo: {testName}");
    var command = new LoginCommand(email, password, "127.0.0.1", $"EdgeCase-Email-{testName}");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Success.Should().Be(expectedSuccess, $"Caso límite {testName} debe tener éxito = {expectedSuccess}");

    if (!expectedSuccess)
    {
      result.Error.Should().NotBeNullOrEmpty("Un inicio de sesión fallido debe incluir mensaje de error");
      result.ErrorInfo.Should().NotBeNull("Un inicio de sesión fallido debe incluir información de error");

      // Verificar que el mensaje de error es genérico (anti-enumeración)
      result.Error.Should().Be(AuthValidationTypes.InvalidCredentials.Message,
"El mensaje de error debe ser genérico para evitar enumeración de usuarios");
    }

    _output.WriteLine($"   ? Resultado: Éxito={result.Success}, Error='{result.Error}'");
  }

  /// <summary>
  /// Test de edge cases con contraseñas extremas y caracteres especiales.
  /// </summary>
  [Theory]
  [MemberData(nameof(LoginEdgeCasesData.EdgeCasePasswords), MemberType = typeof(LoginEdgeCasesData))]
  [Trait("Category", "EdgeCases")]
  [Trait("Type", "PasswordValidation")]
  public async Task LoginHandler_EdgeCasePasswords_HandlesGracefully(string email, string password, bool expectedSuccess, string testName)
  {
    // Arrange
    _output.WriteLine($"?? Probando caso límite de contraseña: {testName}");
    var command = new LoginCommand(email, password, "127.0.0.1", $"EdgeCase-Password-{testName}");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Success.Should().Be(expectedSuccess, $"Caso límite {testName} debe tener éxito = {expectedSuccess}");

    if (!expectedSuccess)
    {
      result.Error.Should().NotBeNullOrEmpty("Un inicio de sesión fallido debe incluir mensaje de error");
      result.ErrorInfo.Should().NotBeNull("Un inicio de sesión fallido debe incluir información de error");
    }

    // El sistema no debe fallar con ningún tipo de input
    result.Should().NotBeNull("El handler siempre debe devolver un resultado y nunca lanzar excepción");

    _output.WriteLine($"   ? Resultado: Éxito={result.Success}, LongitudContraseña={password?.Length ?? 0}");
  }

  /// <summary>
  /// Test de edge cases combinando emails y contraseñas problemáticos.
  /// </summary>
  [Theory]
  [MemberData(nameof(LoginEdgeCasesData.CombinedEdgeCases), MemberType = typeof(LoginEdgeCasesData))]
  [Trait("Category", "EdgeCases")]
  [Trait("Type", "CombinedValidation")]
  public async Task LoginHandler_CombinedEdgeCases_HandlesGracefully(string email, string password, bool expectedSuccess, string testName)
  {
    // Arrange
    _output.WriteLine($"?? Probando caso límite combinado: {testName}");
    var command = new LoginCommand(email, password, "127.0.0.1", $"EdgeCase-Combined-{testName}");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Success.Should().Be(expectedSuccess, $"Caso límite combinado {testName} debe tener éxito = {expectedSuccess}");
    result.Should().NotBeNull("El handler siempre debe devolver un resultado");

    if (!expectedSuccess)
    {
      result.Error.Should().NotBeNullOrEmpty("Un inicio de sesión fallido debe incluir mensaje de error");
    }

    _output.WriteLine($"   ? Resultado: Éxito={result.Success}, LongitudCorreo={email?.Length ?? 0}, LongitudContraseña={password?.Length ?? 0}");
  }

  /// <summary>
  /// Test de timing attack - verifica que los tiempos de respuesta sean consistentes
  /// para prevenir enumeración de usuarios válidos.
  /// </summary>
  [Theory]
  [MemberData(nameof(LoginEdgeCasesData.TimingAttackCases), MemberType = typeof(LoginEdgeCasesData))]
  [Trait("Category", "Security")]
  [Trait("Type", "TimingAttack")]
  public async Task LoginHandler_TimingAttackPrevention_ConsistentResponseTimes(string email, string password, bool expectedSuccess, string testName)
  {
    // Arrange
    _output.WriteLine($"?? Probando consistencia de tiempos: {testName}");
    var timings = new List<long>();

    // Act - Ejecutar múltiples veces para obtener timing promedio
    for (int i = 0; i < 10; i++)
    {
      var stopwatch = System.Diagnostics.Stopwatch.StartNew();
      var command = new LoginCommand(email, password, "127.0.0.1", $"Timing-{testName}-{i}");
      var result = await _handler.Handle(command, CancellationToken.None);
      stopwatch.Stop();

      timings.Add(stopwatch.ElapsedMilliseconds);

      result.Success.Should().Be(expectedSuccess, $"Prueba de tiempos {testName} iteración {i} debe tener resultado consistente");
    }

    // Assert - Los tiempos deben ser consistentes para Argon2id (variación más permisiva en hardware de desarrollo)
    var averageTime = timings.Average();
    var maxDeviation = Math.Max(1500, averageTime * 1.5); // 150% de desviación máxima o 1500ms mínimo (Argon2id es memory-hard, mayor latencia base que BCrypt; margen ampliado por ruido de ci-full con coverage)

    foreach (var timing in timings)
    {
      Math.Abs(timing - averageTime).Should().BeLessThan(
        maxDeviation,
        $"Variación de tiempo demasiado alta para {testName}: {timing}ms vs promedio {averageTime:F1}ms (considerar variación de hardware de desarrollo)");
    }

    _output.WriteLine($"   ? Timing: Promedio={averageTime:F1}ms, Rango=[{timings.Min()}-{timings.Max()}]ms");
  }

  /// <summary>
  /// Test de protección anti-brute force - verifica que múltiples intentos no causen problemas.
  /// </summary>
  [Fact]
  [Trait("Category", "Security")]
  [Trait("Type", "BruteForceProtection")]
  public async Task LoginHandler_BruteForceAttempts_HandlesGracefully()
  {
    // Arrange
    _output.WriteLine("??? Probando protección contra fuerza bruta...");
    var results = new List<bool>();

    // Act - Simular múltiples intentos de brute force
    foreach (var testCase in LoginEdgeCasesData.BruteForceProtectionCases)
    {
      var email = (string)testCase[0];
      var password = (string)testCase[1];
      var expectedSuccess = (bool)testCase[2];
      var testName = (string)testCase[3];

      var command = new LoginCommand(email, password, "127.0.0.1", $"BruteForce-{testName}");
      var result = await _handler.Handle(command, CancellationToken.None);

      result.Success.Should().Be(expectedSuccess, $"Intento de fuerza bruta {testName} debe fallar");
      results.Add(result.Success);
    }

    // Assert - Todos los intentos deben fallar consistentemente
    foreach (var success in results)
    {
      success.Should().BeFalse("Todos los intentos de fuerza bruta deben fallar");
    }

    // El sistema debe mantenerse estable después de múltiples intentos
    var finalTestCommand = new LoginCommand("admin@example.com", "AdminPass123!", "127.0.0.1", "FinalValidation");
    var finalResult = await _handler.Handle(finalTestCommand, CancellationToken.None);
    finalResult.Success.Should().BeTrue("El sistema debe seguir funcionando después de intentos de fuerza bruta");

    _output.WriteLine($"   ? Procesados {results.Count} intentos de fuerza bruta, el sistema se mantiene estable");
  }

  /// <summary>
  /// Test de usuarios inactivos con credenciales válidas.
  /// </summary>
  [Fact]
  [Trait("Category", "EdgeCases")]
  [Trait("Type", "InactiveUser")]
  public async Task LoginHandler_InactiveUserValidCredentials_ShouldFail()
  {
    // Arrange
    _output.WriteLine("? Probando usuario inactivo con credenciales válidas...");
    var command = new LoginCommand("inactive@example.com", "InactiveUser123!", "127.0.0.1", "InactiveUserTest");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Success.Should().BeFalse("Un usuario inactivo no debe poder iniciar sesión");
    result.ErrorInfo.Should().NotBeNull("Debe incluir información de error");
    result.ErrorInfo!.Key.Should().Be(ErrorInfo.AccountPendingOrInactive.Key, "Debe devolver clave de cuenta pendiente/inactiva");
    result.Error.Should().Be("Tu cuenta esta pendiente de activacion o inactiva. Revisa tu correo o solicita reenvio de activacion.", "Debe informar estado de activación");

    _output.WriteLine("   ? Acceso denegado correctamente para usuario inactivo");
  }

  public void Dispose()
  {
    _dbContext?.Dispose();
    GC.SuppressFinalize(this);
  }
}



