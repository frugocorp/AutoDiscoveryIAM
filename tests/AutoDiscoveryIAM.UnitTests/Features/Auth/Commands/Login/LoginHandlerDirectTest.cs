// -----------------------------------------------------------------------
// <copyright file="LoginHandlerDirectTest.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login;

/// <summary>
/// Test espec�fico para identificar exactamente d�nde falla el LoginHandler.
/// </summary>
public class LoginHandlerDirectTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AutoDiscoveryIAMDbContext _dbContext;
    private readonly LoginHandler _handler;
    private readonly PasswordHasher _passwordHasher;

    public LoginHandlerDirectTest(ITestOutputHelper output)
    {
        _output = output;

        // Setup exactamente igual que LoginPerformanceTests
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
          .UseInMemoryDatabase(databaseName: $"LoginDirectTestDb_{Guid.NewGuid()}")
          .Options;

        _dbContext = new AutoDiscoveryIAMDbContext(options);

        // Configurar servicios
        _passwordHasher = new PasswordHasher();
        var authLogger = Substitute.For<IAuthLogger>();
        var refreshTokenService = Substitute.For<IRefreshTokenService>();
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

        // Configurar mock del refresh token service (basado en LoginHandlerTests.cs)
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
          authLogger, _dbContext, _passwordHasher, refreshTokenService, refreshTokenRepository,
          jwtClaimsFactory, unitOfWork, lockoutSettings,
          Substitute.For<ITwoFactorCodeGenerator>(), Substitute.For<ITwoFactorEmailService>(), Options.Create(new TwoFactorSettings()),
          Substitute.For<ISecurityEventLogger>());

        // Sembrar datos de prueba exactamente igual
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Exactamente los mismos datos que LoginPerformanceTests pero con todos los campos requeridos
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
            UserRoles = new List<UserRole>(), // Inicializar collection vac�a
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
            UserRoles = new List<UserRole>(), // Inicializar collection vac�a
        };

        _dbContext.Users.AddRange(adminUser, regularUser);
        _dbContext.SaveChanges();

        _output.WriteLine($"? Usuarios creados:");
        _output.WriteLine($"   - {adminUser.Email} (ID: {adminUser.Id}, Active: {adminUser.IsActive}, Username: '{adminUser.Username}')");
        _output.WriteLine($"   - {regularUser.Email} (ID: {regularUser.Id}, Active: {regularUser.IsActive}, Username: '{regularUser.Username}')");
    }

    [Fact]
    public async Task LoginHandler_AdminLogin_ExactSameAsPerformanceTest()
    {
        // Arrange - usar exactamente los mismos datos que en el performance test que falla
        _output.WriteLine("?? Probando exactamente los mismos datos que la prueba de rendimiento...");
        var command = new LoginCommand("admin@example.com", "AdminPass123!", "127.0.0.1", "Performance_Valid_1_0");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _output.WriteLine($"Result.Success: {result.Success}");
        if (!result.Success)
        {
            _output.WriteLine($"Error: {result.Error}");
            _output.WriteLine($"ErrorInfo: {result.ErrorInfo}");
        }
        else
        {
            _output.WriteLine($"? Login successful!");
        }

        result.Success.Should().BeTrue("Admin login with correct password debe ser exitoso");
    }

    [Fact]
    public async Task LoginHandler_CheckUserExistsInDatabase()
    {
        // Verificar que los usuarios existen en la DB
        var adminUser = await _dbContext.Users
          .FirstOrDefaultAsync(u => u.Email == "admin@example.com" && !u.IsDeleted, CancellationToken.None);

        var regularUser = await _dbContext.Users
          .FirstOrDefaultAsync(u => u.Email == "user@example.com" && !u.IsDeleted, CancellationToken.None);

        _output.WriteLine($"Admin found: {adminUser?.Email} (Active: {adminUser?.IsActive})");
        _output.WriteLine($"User found: {regularUser?.Email} (Active: {regularUser?.IsActive})");

        adminUser.Should().NotBeNull("El usuario administrador debe existir");
        regularUser.Should().NotBeNull("El usuario regular debe existir");
        adminUser!.IsActive.Should().BeTrue("Admin debe estar activo");
        regularUser!.IsActive.Should().BeTrue("El usuario regular debe estar activo");
    }

    [Fact]
    public void PasswordHasher_VerifyCorrectPassword()
    {
        // Verificar que el password service funciona
        const string password = "AdminPass123!";
        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        _output.WriteLine($"Password: {password}");
        _output.WriteLine($"Hash starts with: {hash[..20]}...");
        _output.WriteLine($"Verification: {isValid}");

        isValid.Should().BeTrue("La verificaci�n de contrase�a debe funcionar");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}



