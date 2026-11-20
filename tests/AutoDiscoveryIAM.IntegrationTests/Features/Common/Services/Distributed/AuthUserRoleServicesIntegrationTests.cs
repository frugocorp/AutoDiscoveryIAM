using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.RoleQuery;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Common.Services.Distributed;

/// <summary>
/// Tests de integración entre AuthContextService, UserQueryService y RoleQueryService.
/// Valida el flujo completo de autorización distribuida usando servicios reales.
/// </summary>
public class AuthUserRoleServicesIntegrationTests : IDisposable
{
    private readonly AutoDiscoveryIAMDbContext _context;
    private readonly AuthContextService _authContextService;
    private readonly UserQueryService _userQueryService;
    private readonly RoleQueryService _roleQueryService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthUserRoleServicesIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuthUserRoleServicesIntegrationTests_{Guid.NewGuid()}")
            .Options;

        _context = new AutoDiscoveryIAMDbContext(options);
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        _authContextService = new AuthContextService(_httpContextAccessor, _context);
        _userQueryService = new UserQueryService(_context);
        _roleQueryService = new RoleQueryService(_context);
    }

    [Fact]
    public async Task CompleteAuthorizationFlow_FromJwtToPermissions_WorksCorrectly()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "admin@example.com";
        var user = CreateTestUser(userPublicId, email);

        var adminRole = CreateTestRole("Admin", "Administrator role");
        var userRole = CreateTestRole("User", "Standard user role");

        var permissions = new[]
        {
            CreateTestPermission("posts.read", "Read posts"),
            CreateTestPermission("posts.write", "Write posts"),
            CreateTestPermission("posts.delete", "Delete posts"),
            CreateTestPermission("users.read", "Read users"),
        };

        // Setup database
        _context.Users.Add(user);
        _context.Roles.AddRange(adminRole, userRole);
        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = adminRole.Id },
            new UserRole { UserId = user.Id, RoleId = userRole.Id });

        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[0].Id }, // posts.read
            new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[1].Id }, // posts.write
            new RolePermission { RoleId = adminRole.Id, PermissionId = permissions[2].Id }, // posts.delete
            new RolePermission { RoleId = userRole.Id, PermissionId = permissions[0].Id },  // posts.read
            new RolePermission { RoleId = userRole.Id, PermissionId = permissions[3].Id }); // users.read

        await _context.SaveChangesAsync(CancellationToken.None);

        // Setup JWT claims
        SetupHttpContextWithUser(userPublicId.ToString(), email);

        // Act & Assert - Step 1: Get current user from JWT
        var currentUserPublicId = _authContextService.GetCurrentUserPublicId();
        currentUserPublicId.Should().Be(userPublicId);

        var isAuthenticated = _authContextService.IsAuthenticated();
        isAuthenticated.Should().BeTrue();

        var currentUser = await _authContextService.GetCurrentUserAsync();
        currentUser.Should().NotBeNull();
        currentUser!.Email.Should().Be(email);

        // Act & Assert - Step 2: Get user roles through UserQueryService
        var userRoles = await _userQueryService.GetUserRolesByPublicIdAsync(userPublicId);
        userRoles.Count.Should().Be(2);
        userRoles.Should().Contain(r => r.Name == "Admin");
        userRoles.Should().Contain(r => r.Name == "User");

        // Act & Assert - Step 3: Get user permissions through UserQueryService
        var userPermissions = await _userQueryService.GetUserPermissionsByPublicIdAsync(userPublicId);
        userPermissions.Count.Should().Be(4);
        userPermissions.Should().Contain("posts.read");
        userPermissions.Should().Contain("posts.write");
        userPermissions.Should().Contain("posts.delete");
        userPermissions.Should().Contain("users.read");

        // Act & Assert - Step 4: Check specific permissions through UserQueryService
        var canReadPosts = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "posts.read");
        var canDeletePosts = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "posts.delete");
        var canManageSystem = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "system.manage");

        canReadPosts.Should().BeTrue();
        canDeletePosts.Should().BeTrue();
        canManageSystem.Should().BeFalse();

        // Act & Assert - Step 5: Verify role permissions through RoleQueryService
        var adminRolePermissions = await _roleQueryService.GetRolePermissionsByPublicIdAsync(adminRole.PublicId);
        adminRolePermissions.Count.Should().Be(3);
        adminRolePermissions.Should().Contain("posts.read");
        adminRolePermissions.Should().Contain("posts.write");
        adminRolePermissions.Should().Contain("posts.delete");

        var userRolePermissions = await _roleQueryService.GetRolePermissionsByPublicIdAsync(userRole.PublicId);
        userRolePermissions.Count.Should().Be(2);
        userRolePermissions.Should().Contain("posts.read");
        userRolePermissions.Should().Contain("users.read");
    }

    [Fact]
    public async Task AuthorizationFlow_WithInactiveRole_ExcludesInactiveRolePermissions()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "user@example.com";
        var user = CreateTestUser(userPublicId, email);

        var activeRole = CreateTestRole("Active", "Active role", true);
        var inactiveRole = CreateTestRole("Inactive", "Inactive role", false);

        var readPermission = CreateTestPermission("posts.read", "Read posts");
        var writePermission = CreateTestPermission("posts.write", "Write posts");

        _context.Users.Add(user);
        _context.Roles.AddRange(activeRole, inactiveRole);
        _context.Permissions.AddRange(readPermission, writePermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = activeRole.Id },
            new UserRole { UserId = user.Id, RoleId = inactiveRole.Id });

        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = activeRole.Id, PermissionId = readPermission.Id },
            new RolePermission { RoleId = inactiveRole.Id, PermissionId = writePermission.Id });

        await _context.SaveChangesAsync(CancellationToken.None);

        SetupHttpContextWithUser(userPublicId.ToString(), email);

        // Act
        var userRoles = await _userQueryService.GetUserRolesByPublicIdAsync(userPublicId);
        var userPermissions = await _userQueryService.GetUserPermissionsByPublicIdAsync(userPublicId);

        // Assert
        userRoles.Should().ContainSingle();
        userRoles.Should().Contain(r => r.Name == "Active");
        userRoles.Should().NotContain(r => r.Name == "Inactive");

        userPermissions.Should().ContainSingle();
        userPermissions.Should().Contain("posts.read");
        userPermissions.Should().NotContain(perm => perm == "posts.write");
    }

    [Fact]
    public async Task AuthorizationFlow_WithUnauthenticatedUser_HandlesGracefully()
    {
        // Arrange
        SetupHttpContextWithoutUser();

        // Act & Assert
        var isAuthenticated = _authContextService.IsAuthenticated();
        isAuthenticated.Should().BeFalse();

        var currentUserPublicId = _authContextService.GetCurrentUserPublicId();
        currentUserPublicId.Should().BeNull();

        var currentUser = await _authContextService.GetCurrentUserAsync();
        currentUser.Should().BeNull();
    }

    [Fact]
    public async Task CrossServiceIntegration_RoleBasedQueries_WorkCorrectly()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var role = CreateTestRole("Manager", "Manager role");
        role.PublicId = rolePublicId;

        var permissions = new[]
        {
            CreateTestPermission("teams.read", "Read teams"),
            CreateTestPermission("teams.write", "Write teams"),
            CreateTestPermission("reports.read", "Read reports"),
        };

        _context.Roles.Add(role);
        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.RolePermissions.AddRange(permissions.Select(p =>
            new RolePermission { RoleId = role.Id, PermissionId = p.Id }));
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var retrievedRole = await _roleQueryService.GetRoleByPublicIdAsync(rolePublicId);
        var rolePermissions = await _roleQueryService.GetRolePermissionsByPublicIdAsync(rolePublicId);

        // Assert
        retrievedRole.Should().NotBeNull();
        retrievedRole!.Name.Should().Be("Manager");

        rolePermissions.Count.Should().Be(3);
        rolePermissions.Should().Contain("teams.read");
        rolePermissions.Should().Contain("teams.write");
        rolePermissions.Should().Contain("reports.read");
        rolePermissions.Should().NotContain(perm => perm == "users.delete");
    }

    #region Helper Methods

    private void SetupHttpContextWithUser(string publicId, string email)
    {
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", publicId),
            new Claim("email", email),
        }, "Test");

        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private void SetupHttpContextWithoutUser()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
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

    private static Role CreateTestRole(string name, string description, bool isActive = true)
    {
        return new Role
        {
            PublicId = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = isActive,
        };
    }

    private static Permission CreateTestPermission(string key, string description, bool isActive = true)
    {
        return new Permission
        {
            PublicId = Guid.NewGuid(),
            Key = key,
            Description = description,
            IsActive = isActive,
        };
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
