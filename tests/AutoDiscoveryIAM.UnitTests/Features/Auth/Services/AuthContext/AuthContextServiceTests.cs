using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Services.AuthContext;

/// <summary>
/// Pruebas unitarias para AuthContextService, validando la extracción de contexto JWT y consultas de usuario.
/// </summary>
public class AuthContextServiceTests : IDisposable
{
    private readonly AutoDiscoveryIAMDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthContextService _authContextService;

    public AuthContextServiceTests()
    {
        // Configurar base de datos en memoria
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AutoDiscoveryIAMDbContext(options);

        // Setup HttpContextAccessor mock
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        // Create service
        _authContextService = new AuthContextService(_httpContextAccessor, _context);
    }

    /// <summary>
    /// Verifica que GetCurrentUserPublicId() extrae correctamente el PublicId desde los claims del JWT.
    /// </summary>
    [Fact(DisplayName = "GetCurrentUserPublicId extrae PublicId desde claims JWT correctamente")]
    public void GetCurrentUserPublicId_ValidPublicIdClaim_ReturnsGuid()
    {
        // Arrange
        var expectedPublicId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim("PublicId", expectedPublicId.ToString()),
            new Claim("email", "test@example.com"),
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _authContextService.GetCurrentUserPublicId();

        // Assert
        result.Should().Be(expectedPublicId);
    }

    /// <summary>
    /// Verifica que GetCurrentUserPublicId() retorna null cuando no hay usuario autenticado.
    /// </summary>
    [Fact(DisplayName = "GetCurrentUserPublicId retorna null cuando no hay usuario autenticado")]
    public void GetCurrentUserPublicId_NoAuthentication_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _authContextService.GetCurrentUserPublicId();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifica que GetCurrentUserEmail() extrae correctamente el email desde los claims del JWT.
    /// </summary>
    [Fact(DisplayName = "GetCurrentUserEmail extrae email desde claims JWT correctamente")]
    public void GetCurrentUserEmail_ValidEmailClaim_ReturnsEmail()
    {
        // Arrange
        const string expectedEmail = "test@example.com";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, expectedEmail),
            new Claim("PublicId", Guid.NewGuid().ToString()),
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _authContextService.GetCurrentUserEmail();

        // Assert
        result.Should().Be(expectedEmail);
    }

    /// <summary>
    /// Verifica que IsAuthenticated() retorna true cuando el usuario está autenticado.
    /// </summary>
    [Fact(DisplayName = "IsAuthenticated retorna true para usuario autenticado")]
    public void IsAuthenticated_AuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("PublicId", Guid.NewGuid().ToString()),
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _authContextService.IsAuthenticated();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que IsAuthenticated() retorna false cuando no hay usuario autenticado.
    /// </summary>
    [Fact(DisplayName = "IsAuthenticated retorna false para usuario no autenticado")]
    public void IsAuthenticated_NoAuthentication_ReturnsFalse()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _authContextService.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que GetCurrentUserAsync() retorna el usuario completo desde la BD.
    /// </summary>
    [Fact(DisplayName = "GetCurrentUserAsync retorna usuario completo desde BD")]
    public async Task GetCurrentUserAsync_ValidPublicId_ReturnsUser()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        var user = new User
        {
            Id = 1,
            PublicId = userPublicId,
            Email = "test@example.com",
            Username = "testuser",
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim("PublicId", userPublicId.ToString()),
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = await _authContextService.GetCurrentUserAsync();

        // Assert
        result.Should().NotBeNull();
        result!.PublicId.Should().Be(userPublicId);
        result.Email.Should().Be("test@example.com");
        result.Username.Should().Be("testuser");
    }

    /// <summary>
    /// Verifica que GetCurrentUserAsync() retorna null cuando no hay usuario autenticado.
    /// </summary>
    [Fact(DisplayName = "GetCurrentUserAsync retorna null cuando no hay usuario autenticado")]
    public async Task GetCurrentUserAsync_NoAuthentication_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = await _authContextService.GetCurrentUserAsync();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifica que GetCurrentUserClaims() retorna todos los claims del JWT.
    /// </summary>
    [Fact(DisplayName = "GetCurrentUserClaims retorna todos los claims del JWT")]
    public void GetCurrentUserClaims_AuthenticatedUser_ReturnsAllClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("PublicId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim("role", "user"),
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _authContextService.GetCurrentUserClaims();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(c => c.Type == "PublicId");
        result.Should().Contain(c => c.Type == ClaimTypes.Email);
        result.Should().Contain(c => c.Type == "role");
    }

    /// <summary>
    /// Verifica que GetCurrentUserPublicId() funciona con claim "sub" alternativo.
    /// </summary>
    [Fact(DisplayName = "GetCurrentUserPublicId funciona con claim sub alternativo")]
    public void GetCurrentUserPublicId_SubClaim_ReturnsGuid()
    {
        // Arrange
        var expectedPublicId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim("sub", expectedPublicId.ToString()),
            new Claim("email", "test@example.com"),
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _authContextService.GetCurrentUserPublicId();

        // Assert
        result.Should().Be(expectedPublicId);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}



