using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.RoleQuery;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Services.RoleQuery;

/// <summary>
/// Pruebas unitarias para RoleQueryService, validando consultas optimizadas del dominio Roles.
/// </summary>
public class RoleQueryServiceTests : IDisposable
{
    private readonly AutoDiscoveryIAMDbContext _context;
    private readonly RoleQueryService _roleQueryService;

    public RoleQueryServiceTests()
    {
        // Configurar base de datos en memoria
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AutoDiscoveryIAMDbContext(options);

        // Create service
        _roleQueryService = new RoleQueryService(_context);
    }

    /// <summary>
    /// Verifica que GetRolePermissionsByPublicIdAsync() retorna permisos del rol especificado.
    /// </summary>
    [Fact(DisplayName = "GetRolePermissionsByPublicIdAsync retorna permisos del rol especificado")]
    public async Task GetRolePermissionsByPublicIdAsync_ValidRole_ReturnsRolePermissions()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var role = CreateTestRole(rolePublicId, "Admin", "Administrator role");

        var readPermission = CreateTestPermission("posts.read", "Read posts");
        var writePermission = CreateTestPermission("posts.write", "Write posts");
        var deletePermission = CreateTestPermission("posts.delete", "Delete posts");

        _context.Roles.Add(role);
        _context.Permissions.AddRange(readPermission, writePermission, deletePermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assign permissions to role
        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = readPermission.Id },
            new RolePermission { RoleId = role.Id, PermissionId = writePermission.Id },
            new RolePermission { RoleId = role.Id, PermissionId = deletePermission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRolePermissionsByPublicIdAsync(rolePublicId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("posts.read");
        result.Should().Contain("posts.write");
        result.Should().Contain("posts.delete");
    }

    /// <summary>
    /// Verifica que GetRoleByPublicIdAsync() retorna el rol correcto por PublicId.
    /// </summary>
    [Fact(DisplayName = "GetRoleByPublicIdAsync retorna rol por PublicId")]
    public async Task GetRoleByPublicIdAsync_ValidPublicId_ReturnsRole()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var role = CreateTestRole(rolePublicId, "Editor", "Content editor role");

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRoleByPublicIdAsync(rolePublicId);

        // Assert
        result.Should().NotBeNull();
        result!.PublicId.Should().Be(rolePublicId);
        result.Name.Should().Be("Editor");
        result.Description.Should().Be("Content editor role");
    }

    /// <summary>
    /// Verifica que GetRoleByNameAsync() retorna el rol correcto por nombre.
    /// </summary>
    [Fact(DisplayName = "GetRoleByNameAsync retorna rol por nombre")]
    public async Task GetRoleByNameAsync_ValidName_ReturnsRole()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        const string roleName = "Moderator";
        var role = CreateTestRole(rolePublicId, roleName, "Content moderator role");

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRoleByNameAsync(roleName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(roleName);
        result.PublicId.Should().Be(rolePublicId);
        result.Description.Should().Be("Content moderator role");
    }

    /// <summary>
    /// Verifica que GetAllRolesAsync() retorna todos los roles activos ordenados por nombre.
    /// </summary>
    [Fact(DisplayName = "GetAllRolesAsync retorna todos los roles activos ordenados")]
    public async Task GetAllRolesAsync_MultipleRoles_ReturnsActiveRolesOrdered()
    {
        // Arrange
        var adminRole = CreateTestRole(Guid.NewGuid(), "Admin", "Administrator", isActive: true);
        var editorRole = CreateTestRole(Guid.NewGuid(), "Editor", "Content editor", isActive: true);
        var userRole = CreateTestRole(Guid.NewGuid(), "User", "Standard user", isActive: true);
        var inactiveRole = CreateTestRole(Guid.NewGuid(), "Inactive", "Inactive role", isActive: false);

        _context.Roles.AddRange(adminRole, editorRole, userRole, inactiveRole);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetAllRolesAsync();

        // Assert
        result.Should().HaveCount(3); // Solo roles activos
        result.Should().NotContain(r => r.Name == "Inactive");
        result.Should().BeInAscendingOrder(r => r.Name); // Ordenados por nombre

        result[0].Name.Should().Be("Admin");
        result[1].Name.Should().Be("Editor");
        result[2].Name.Should().Be("User");
    }

    /// <summary>
    /// Verifica que GetRolePermissionsByPublicIdAsync() no incluye permisos inactivos.
    /// </summary>
    [Fact(DisplayName = "GetRolePermissionsByPublicIdAsync excluye permisos inactivos")]
    public async Task GetRolePermissionsByPublicIdAsync_InactivePermissions_ExcludesInactivePermissions()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var role = CreateTestRole(rolePublicId, "Test Role", "Test role");

        var activePermission = CreateTestPermission("posts.read", "Read posts", isActive: true);
        var inactivePermission = CreateTestPermission("posts.write", "Write posts", isActive: false);

        _context.Roles.Add(role);
        _context.Permissions.AddRange(activePermission, inactivePermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assign both permissions to role
        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionId = activePermission.Id },
            new RolePermission { RoleId = role.Id, PermissionId = inactivePermission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRolePermissionsByPublicIdAsync(rolePublicId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("posts.read");
        result.Should().NotContain("posts.write");
    }

    /// <summary>
    /// Verifica que GetRolePermissionsByPublicIdAsync() no incluye roles inactivos.
    /// </summary>
    [Fact(DisplayName = "GetRolePermissionsByPublicIdAsync excluye roles inactivos")]
    public async Task GetRolePermissionsByPublicIdAsync_InactiveRole_ReturnsEmpty()
    {
        // Arrange
        var rolePublicId = Guid.NewGuid();
        var inactiveRole = CreateTestRole(rolePublicId, "Inactive Role", "Inactive role", isActive: false);
        var permission = CreateTestPermission("posts.read", "Read posts");

        _context.Roles.Add(inactiveRole);
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.RolePermissions.Add(new RolePermission { RoleId = inactiveRole.Id, PermissionId = permission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _roleQueryService.GetRolePermissionsByPublicIdAsync(rolePublicId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que las consultas retornan null para roles inexistentes.
    /// </summary>
    [Fact(DisplayName = "GetRoleByPublicIdAsync retorna null para rol inexistente")]
    public async Task GetRoleByPublicIdAsync_NonExistentRole_ReturnsNull()
    {
        // Arrange
        var nonExistentPublicId = Guid.NewGuid();

        // Act
        var result = await _roleQueryService.GetRoleByPublicIdAsync(nonExistentPublicId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifica que GetRoleByNameAsync() retorna null para nombre inexistente.
    /// </summary>
    [Fact(DisplayName = "GetRoleByNameAsync retorna null para nombre inexistente")]
    public async Task GetRoleByNameAsync_NonExistentName_ReturnsNull()
    {
        // Arrange
        const string nonExistentName = "NonExistentRole";

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



