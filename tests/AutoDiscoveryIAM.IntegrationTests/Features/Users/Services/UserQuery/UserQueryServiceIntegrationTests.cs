using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Services.UserQuery;

/// <summary>
/// Tests de integración para UserQueryService.
/// Valida consultas complejas de usuarios con roles y permisos usando base de datos real.
/// </summary>
public class UserQueryServiceIntegrationTests : IDisposable
{
    private readonly AutoDiscoveryIAMDbContext _context;
    private readonly UserQueryService _userQueryService;

    public UserQueryServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserQueryServiceIntegrationTests_{Guid.NewGuid()}")
            .Options;

        _context = new AutoDiscoveryIAMDbContext(options);
        _userQueryService = new UserQueryService(_context);
    }

    [Fact]
    public async Task GetUserRolesByPublicIdAsync_WithUserHavingMultipleRoles_ReturnsAllActiveRoles()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "test@example.com";
        var user = CreateTestUser(userPublicId, email);

        var adminRole = CreateTestRole("Admin", "Administrator role");
        var userRole = CreateTestRole("User", "Standard user role");
        var inactiveRole = CreateTestRole("Inactive", "Inactive role", false);

        _context.Users.Add(user);
        _context.Roles.AddRange(adminRole, userRole, inactiveRole);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = adminRole.Id },
            new UserRole { UserId = user.Id, RoleId = userRole.Id },
            new UserRole { UserId = user.Id, RoleId = inactiveRole.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserRolesByPublicIdAsync(userPublicId);

        // Assert
        result.Count.Should().Be(2);
        result.Should().Contain(r => r.Name == "Admin");
        result.Should().Contain(r => r.Name == "User");
        result.Should().NotContain(r => r.Name == "Inactive");
    }

    [Fact]
    public async Task GetUserPermissionsByPublicIdAsync_WithComplexRolePermissions_ReturnsDistinctPermissions()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "test@example.com";
        var user = CreateTestUser(userPublicId, email);

        var adminRole = CreateTestRole("Admin", "Administrator role");
        var userRole = CreateTestRole("User", "Standard user role");

        var readPermission = CreateTestPermission("posts.read", "Read posts");
        var writePermission = CreateTestPermission("posts.write", "Write posts");
        var deletePermission = CreateTestPermission("posts.delete", "Delete posts");

        _context.Users.Add(user);
        _context.Roles.AddRange(adminRole, userRole);
        _context.Permissions.AddRange(readPermission, writePermission, deletePermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Both roles have read permission (should not duplicate)
        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = adminRole.Id, PermissionId = readPermission.Id },
            new RolePermission { RoleId = adminRole.Id, PermissionId = writePermission.Id },
            new RolePermission { RoleId = adminRole.Id, PermissionId = deletePermission.Id },
            new RolePermission { RoleId = userRole.Id, PermissionId = readPermission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = adminRole.Id },
            new UserRole { UserId = user.Id, RoleId = userRole.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserPermissionsByPublicIdAsync(userPublicId);

        // Assert
        result.Count.Should().Be(3);
        result.Should().Contain("posts.read");
        result.Should().Contain("posts.write");
        result.Should().Contain("posts.delete");
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WithExistingPermission_ReturnsTrue()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "test@example.com";
        var user = CreateTestUser(userPublicId, email);

        var adminRole = CreateTestRole("Admin", "Administrator role");
        var readPermission = CreateTestPermission("posts.read", "Read posts");

        _context.Users.Add(user);
        _context.Roles.Add(adminRole);
        _context.Permissions.Add(readPermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.RolePermissions.Add(
            new RolePermission { RoleId = adminRole.Id, PermissionId = readPermission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.Add(
            new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "posts.read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WithNonExistingPermission_ReturnsFalse()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "test@example.com";
        var user = CreateTestUser(userPublicId, email);

        var userRole = CreateTestRole("User", "Standard user role");
        var readPermission = CreateTestPermission("posts.read", "Read posts");

        _context.Users.Add(user);
        _context.Roles.Add(userRole);
        _context.Permissions.Add(readPermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.RolePermissions.Add(
            new RolePermission { RoleId = userRole.Id, PermissionId = readPermission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.Add(
            new UserRole { UserId = user.Id, RoleId = userRole.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "posts.delete");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserByPublicIdAsync_WithValidPublicId_ReturnsUser()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "test@example.com";
        var user = CreateTestUser(userPublicId, email);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserByPublicIdAsync(userPublicId);

        // Assert
        result.Should().NotBeNull();
        result!.PublicId.Should().Be(userPublicId);
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        const string email = "test@example.com";
        var user = CreateTestUser(userPublicId, email);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        result.PublicId.Should().Be(userPublicId);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNonExistingEmail_ReturnsNull()
    {
        // Arrange
        const string nonExistentEmail = "nonexistent@example.com";

        // Act
        var result = await _userQueryService.GetUserByEmailAsync(nonExistentEmail);

        // Assert
        result.Should().BeNull();
    }

    #region Helper Methods

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

    private static Permission CreateTestPermission(string key, string description)
    {
        return new Permission
        {
            PublicId = Guid.NewGuid(),
            Key = key,
            Description = description,
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
