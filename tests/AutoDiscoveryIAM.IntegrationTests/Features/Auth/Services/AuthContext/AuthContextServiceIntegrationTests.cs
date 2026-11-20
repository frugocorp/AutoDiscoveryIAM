using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Services.AuthContext;

/// <summary>
/// Tests de integraci�n para AuthContextService.
/// Valida la extracci�n correcta de informaci�n del usuario desde JWT usando base de datos real.
/// </summary>
public class AuthContextServiceIntegrationTests : IDisposable
{
  private readonly AutoDiscoveryIAMDbContext _context;
  private readonly HttpContextAccessor _httpContextAccessor;
  private readonly AuthContextService _authContextService;

  public AuthContextServiceIntegrationTests()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
        .UseInMemoryDatabase(databaseName: $"AuthContextServiceIntegrationTests_{Guid.NewGuid()}")
        .Options;

    _context = new AutoDiscoveryIAMDbContext(options);
    _httpContextAccessor = new HttpContextAccessor();
    _authContextService = new AuthContextService(_httpContextAccessor, _context);
  }

  [Fact]
  public async Task GetCurrentUserAsync_WithValidJwtClaims_ReturnsUserFromDatabase()
  {
    // Arrange
    var userPublicId = Guid.NewGuid();
    const string email = "test@example.com";

    var user = CreateTestUser(userPublicId, email);
    _context.Users.Add(user);
    await _context.SaveChangesAsync(CancellationToken.None);

    var claims = new List<Claim>
        {
            new Claim("PublicId", userPublicId.ToString()),
            new Claim(ClaimTypes.Email, email),
        };

    SetupHttpContext(claims);

    // Act
    var result = await _authContextService.GetCurrentUserAsync();

    // Assert
    result.Should().NotBeNull();
    result!.PublicId.Should().Be(userPublicId);
    result.Email.Should().Be(email);
  }

  [Fact]
  public async Task GetCurrentUserAsync_WithInvalidPublicId_ReturnsNull()
  {
    // Arrange
    var nonExistentPublicId = Guid.NewGuid();

    var claims = new List<Claim>
        {
            new Claim("PublicId", nonExistentPublicId.ToString()),
            new Claim(ClaimTypes.Email, "nonexistent@example.com"),
        };

    SetupHttpContext(claims);

    // Act
    var result = await _authContextService.GetCurrentUserAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void GetCurrentUserPublicId_WithValidClaims_ReturnsPublicId()
  {
    // Arrange
    var expectedPublicId = Guid.NewGuid();

    var claims = new List<Claim>
        {
            new Claim("PublicId", expectedPublicId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
        };

    SetupHttpContext(claims);

    // Act
    var result = _authContextService.GetCurrentUserPublicId();

    // Assert
    result.Should().Be(expectedPublicId);
  }

  [Fact]
  public void GetCurrentUserEmail_WithValidClaims_ReturnsEmail()
  {
    // Arrange
    const string expectedEmail = "test@example.com";

    var claims = new List<Claim>
        {
            new Claim("PublicId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, expectedEmail),
        };

    SetupHttpContext(claims);

    // Act
    var result = _authContextService.GetCurrentUserEmail();

    // Assert
    result.Should().Be(expectedEmail);
  }

  [Fact]
  public void IsAuthenticated_WithValidClaims_ReturnsTrue()
  {
    // Arrange
    var claims = new List<Claim>
        {
            new Claim("PublicId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
        };

    SetupHttpContext(claims);

    // Act
    var result = _authContextService.IsAuthenticated();

    // Assert
    result.Should().BeTrue();
  }

  [Fact]
  public void IsAuthenticated_WithoutHttpContext_ReturnsFalse()
  {
    // Arrange - No setup HttpContext

    // Act
    var result = _authContextService.IsAuthenticated();

    // Assert
    result.Should().BeFalse();
  }

  [Fact]
  public void GetCurrentUserPublicId_WithoutHttpContext_ReturnsNull()
  {
    // Arrange - No setup HttpContext

    // Act
    var result = _authContextService.GetCurrentUserPublicId();

    // Assert
    result.Should().BeNull();
  }

  #region Helper Methods

  private void SetupHttpContext(List<Claim> claims)
  {
    var identity = new ClaimsIdentity(claims, "jwt");
    var principal = new ClaimsPrincipal(identity);

    var httpContext = new DefaultHttpContext
    {
      User = principal,
    };

    _httpContextAccessor.HttpContext = httpContext;
  }

  private static User CreateTestUser(Guid publicId, string email)
  {
    return new User
    {
      PublicId = publicId,
      Email = email,
      Username = email.Split('@')[0],
      PasswordHash = "hashed_password",
      IsActive = true,
    };
  }

  #endregion

  public void Dispose()
  {
    _context.Dispose();
    GC.SuppressFinalize(this);
  }
}
