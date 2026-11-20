using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.RoleQuery;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Roles.Services.RoleQuery;

/// <summary>
/// Tests de integración para RoleQueryService.
/// Valida consultas complejas de roles con permisos usando base de datos real.
/// </summary>
public class RoleQueryServiceIntegrationTests : IDisposable
{
    private readonly AutoDiscoveryIAMDbContext _context;
    private readonly RoleQueryService _roleQueryService;

    public RoleQueryServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: $"RoleQueryServiceIntegrationTests_{Guid.NewGuid()}")
            .Options;

        _context = new AutoDiscoveryIAMDbContext(options);
        _roleQueryService = new RoleQueryService(_context);
    }

    [Fact]
    public async Task GetRoleByPublicIdAsync_WithValidPublicId_ReturnsRole()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var role = CreateTestRole(rolePublicId, "Admin", "Administrator role");

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRoleByPublicIdAsync(rolePublicId);

        // Assert
        result.Should().NotBeNull();
        result!.PublicId.Should().Be(rolePublicId);
        result.Name.Should().Be("Admin");
        result.Description.Should().Be("Administrator role");
    }

    [Fact]
    public async Task GetRoleByPublicIdAsync_WithInvalidPublicId_ReturnsNull()
    {
        // Arrange
        var nonExistentPublicId = Guid.NewGuid();

        // Act
        var result = await _roleQueryService.GetRoleByPublicIdAsync(nonExistentPublicId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllRolesAsync_WithMultipleRoles_ReturnsOnlyActiveRoles()
    {
        // Arrange
        var adminRole = CreateTestRole(Guid.NewGuid(), "Admin", "Administrator role");
        var userRole = CreateTestRole(Guid.NewGuid(), "User", "Standard user role");
        var inactiveRole = CreateTestRole(Guid.NewGuid(), "Inactive", "Inactive role", false);

        _context.Roles.AddRange(adminRole, userRole, inactiveRole);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetAllRolesAsync();

        // Assert
        result.Count.Should().Be(2);
        result.Should().Contain(r => r.Name == "Admin");
        result.Should().Contain(r => r.Name == "User");
        result.Should().NotContain(r => r.Name == "Inactive");
    }

    [Fact]
    public async Task GetRolePermissionsByPublicIdAsync_WithComplexPermissions_ReturnsAllPermissionKeys()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var adminRole = CreateTestRole(rolePublicId, "Admin", "Administrator role");

        var permissions = new[]
        {
            CreateTestPermission("posts.read", "Read posts"),
            CreateTestPermission("posts.write", "Write posts"),
            CreateTestPermission("posts.delete", "Delete posts"),
            CreateTestPermission("users.read", "Read users"),
            CreateTestPermission("users.write", "Write users"),
        };

        _context.Roles.Add(adminRole);
        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync(CancellationToken.None);

        var rolePermissions = permissions.Select(p => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = p.Id,
        });

        _context.RolePermissions.AddRange(rolePermissions);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRolePermissionsByPublicIdAsync(rolePublicId);

        // Assert
        result.Count.Should().Be(5);
        result.Should().Contain("posts.read");
        result.Should().Contain("posts.write");
        result.Should().Contain("posts.delete");
        result.Should().Contain("users.read");
        result.Should().Contain("users.write");
    }

    [Fact]
    public async Task GetRolePermissionsByPublicIdAsync_WithInactivePermissions_ReturnsOnlyActivePermissions()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var adminRole = CreateTestRole(rolePublicId, "Admin", "Administrator role");

        var activePermission = CreateTestPermission("posts.read", "Read posts");
        var inactivePermission = CreateTestPermission("posts.delete", "Delete posts", false);

        _context.Roles.Add(adminRole);
        _context.Permissions.AddRange(activePermission, inactivePermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = adminRole.Id, PermissionId = activePermission.Id },
            new RolePermission { RoleId = adminRole.Id, PermissionId = inactivePermission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRolePermissionsByPublicIdAsync(rolePublicId);

        // Assert
        result.Should().ContainSingle();
        result.Should().Contain("posts.read");
        result.Should().NotContain(perm => perm == "posts.delete");
    }

    [Fact]
    public async Task GetRolePermissionsByPublicIdAsync_WithNonExistentRole_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentRolePublicId = Guid.NewGuid();

        // Act
        var result = await _roleQueryService.GetRolePermissionsByPublicIdAsync(nonExistentRolePublicId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoleByNameAsync_WithValidName_ReturnsRole()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var role = CreateTestRole(rolePublicId, "Manager", "Manager role");

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRoleByNameAsync("Manager");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Manager");
        result.PublicId.Should().Be(rolePublicId);
    }

    [Fact]
    public async Task GetRoleByNameAsync_WithInvalidName_ReturnsNull()
    {
        // Arrange
        const string nonExistentName = "NonExistent";

        // Act
        var result = await _roleQueryService.GetRoleByNameAsync(nonExistentName);

        // Assert
        result.Should().BeNull();
    }

    #region Helper Methods

    private static Role CreateTestRole(Guid publicId, string name, string description, bool isActive = true)
    {
        return new Role
        {
            PublicId = publicId,
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
