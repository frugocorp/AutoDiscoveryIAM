using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Services;

/// <summary>
/// Pruebas unitarias para JwtClaimsFactory, servicio único de generación de claims/access token
/// reutilizado por Login, ExternalLogin y RefreshToken (issue #065).
/// </summary>
public class JwtClaimsFactoryTests
{
  private readonly JwtClaimsFactory _factory;

  public JwtClaimsFactoryTests()
  {
    var settings = new JwtSettings
    {
      Key = "ThisIsAVeryLongSecretKeyForTestingPurposesWithAtLeast256BitsAbc",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpiryMinutes = 60,
    };
    var options = Substitute.For<IOptions<JwtSettings>>();
    options.Value.Returns(settings);
    _factory = new JwtClaimsFactory(options);
  }

  private static User BuildUser(params string[] roleNames)
  {
    var user = new User
    {
      Id = 1,
      PublicId = Guid.NewGuid(),
      Username = "claims-user",
      Email = "claims-user@test.local",
      PasswordHash = "hash",
      IsActive = true,
      UserRoles = new List<UserRole>(),
    };

    foreach (var roleName in roleNames)
    {
      user.UserRoles.Add(new UserRole
      {
        Role = new Role { Name = roleName },
      });
    }

    return user;
  }

  [Fact]
  public void BuildClaims_ShouldIncludeCoreClaims()
  {
    var user = BuildUser();

    var claims = _factory.BuildClaims(user);

    claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.PublicId.ToString());
    claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Username);
  }

  [Fact]
  public void BuildClaims_ShouldIncludeOnlyActiveRoles()
  {
    var user = BuildUser("Admin", "Editor");
    user.UserRoles.Add(new UserRole { IsDeleted = true, Role = new Role { Name = "DeletedRole" } });
    user.UserRoles.Add(new UserRole { Role = new Role { Name = "DeletedRoleEntity", IsDeleted = true } });

    var claims = _factory.BuildClaims(user);

    var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    roleClaims.Should().Contain("Admin");
    roleClaims.Should().Contain("Editor");
    roleClaims.Should().NotContain("DeletedRole");
    roleClaims.Should().NotContain("DeletedRoleEntity");
  }

  [Fact]
  public void BuildAccessToken_ShouldReturnValidJwtWithExpectedClaims()
  {
    var user = BuildUser("Admin");

    var (token, expiresAt) = _factory.BuildAccessToken(user);

    token.Should().NotBeNullOrWhiteSpace();
    expiresAt.Should().BeAfter(DateTime.UtcNow);

    var jwtHandler = new JwtSecurityTokenHandler();
    var jwt = jwtHandler.ReadJwtToken(token);

    jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
    jwt.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == user.PublicId.ToString());
    jwt.Issuer.Should().Be("TestIssuer");
    jwt.Audiences.Should().Contain("TestAudience");
  }
}
