using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Services.UserQuery;

/// <summary>
/// Pruebas unitarias para UserQueryService, validando consultas optimizadas del dominio Users.
/// </summary>
public class UserQueryServiceTests : IDisposable
{
    private readonly AutoDiscoveryIAMDbContext _context;
    private readonly UserQueryService _userQueryService;

    public UserQueryServiceTests()
    {
        // Configurar base de datos en memoria
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AutoDiscoveryIAMDbContext(options);

        // Create service
        _userQueryService = new UserQueryService(_context);
    }

    /// <summary>
    /// Verifica que GetUserRolesByPublicIdAsync() retorna los roles de un usuario específico.
    /// </summary>
    [Fact(DisplayName = "GetUserRolesByPublicIdAsync retorna roles del usuario especificado")]
    public async Task GetUserRolesByPublicIdAsync_ValidUser_ReturnsUserRoles()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        var user = CreateTestUser(userPublicId, "test@example.com");
        var adminRole = CreateTestRole("Admin", "Administrator role");
        var userRole = CreateTestRole("User", "Standard user role");

        _context.Users.Add(user);
        _context.Roles.AddRange(adminRole, userRole);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assign roles to user
        _context.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = adminRole.Id },
            new UserRole { UserId = user.Id, RoleId = userRole.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserRolesByPublicIdAsync(userPublicId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Admin");
        result.Should().Contain(r => r.Name == "User");
    }

    /// <summary>
    /// Verifica que GetUserPermissionsByPublicIdAsync() retorna permisos efectivos del usuario.
    /// </summary>
    [Fact(DisplayName = "GetUserPermissionsByPublicIdAsync retorna permisos efectivos del usuario")]
    public async Task GetUserPermissionsByPublicIdAsync_ValidUser_ReturnsEffectivePermissions()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        var user = CreateTestUser(userPublicId, "test@example.com");
        var adminRole = CreateTestRole("Admin", "Administrator role");

        var readPermission = CreateTestPermission("posts.read", "Read posts");
        var writePermission = CreateTestPermission("posts.write", "Write posts");
        var wildcardPermission = CreateTestPermission("users.*", "All users permissions");

        _context.Users.Add(user);
        _context.Roles.Add(adminRole);
        _context.Permissions.AddRange(readPermission, writePermission, wildcardPermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Assign role to user
        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });

        // Assign permissions to role
        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = adminRole.Id, PermissionId = readPermission.Id },
            new RolePermission { RoleId = adminRole.Id, PermissionId = writePermission.Id },
            new RolePermission { RoleId = adminRole.Id, PermissionId = wildcardPermission.Id });

        // Add more permissions that match wildcard
        var userCreatePermission = CreateTestPermission("users.create", "Create users");
        var userDeletePermission = CreateTestPermission("users.delete", "Delete users");
        _context.Permissions.AddRange(userCreatePermission, userDeletePermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserPermissionsByPublicIdAsync(userPublicId);

        // Assert
        result.Should().Contain("posts.read");
        result.Should().Contain("posts.write");
        result.Should().Contain("users.*");
        result.Should().Contain("users.create");
        result.Should().Contain("users.delete");
    }

    /// <summary>
    /// Verifica que HasPermissionByPublicIdAsync() valida permisos directos correctamente.
    /// </summary>
    [Fact(DisplayName = "HasPermissionByPublicIdAsync valida permisos directos correctamente")]
    public async Task HasPermissionByPublicIdAsync_DirectPermission_ReturnsTrue()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        var user = CreateTestUser(userPublicId, "test@example.com");
        var role = CreateTestRole("Editor", "Content editor role");
        var permission = CreateTestPermission("posts.edit", "Edit posts");

        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "posts.edit");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que HasPermissionByPublicIdAsync() valida permisos wildcards correctamente.
    /// </summary>
    [Fact(DisplayName = "HasPermissionByPublicIdAsync valida permisos wildcards correctamente")]
    public async Task HasPermissionByPublicIdAsync_WildcardPermission_ReturnsTrue()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        var user = CreateTestUser(userPublicId, "test@example.com");
        var role = CreateTestRole("Admin", "Administrator role");
        var wildcardPermission = CreateTestPermission("posts.*", "All posts permissions");

        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.Permissions.Add(wildcardPermission);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = wildcardPermission.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.HasPermissionByPublicIdAsync(userPublicId, "posts.create");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que GetUserByPublicIdAsync() retorna el usuario correcto.
    /// </summary>
    [Fact(DisplayName = "GetUserByPublicIdAsync retorna usuario por PublicId")]
    public async Task GetUserByPublicIdAsync_ValidPublicId_ReturnsUser()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        var user = CreateTestUser(userPublicId, "test@example.com");

        _context.Users.Add(user);
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserByPublicIdAsync(userPublicId);

        // Assert
        result.Should().NotBeNull();
        result!.PublicId.Should().Be(userPublicId);
        result.Email.Should().Be("test@example.com");
    }

    /// <summary>
    /// Verifica que GetUserByEmailAsync() retorna el usuario correcto.
    /// </summary>
    [Fact(DisplayName = "GetUserByEmailAsync retorna usuario por email")]
    public async Task GetUserByEmailAsync_ValidEmail_ReturnsUser()
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

    /// <summary>
    /// Verifica que las consultas no retornan roles inactivos.
    /// </summary>
    [Fact(DisplayName = "GetUserRolesByPublicIdAsync no incluye roles inactivos")]
    public async Task GetUserRolesByPublicIdAsync_InactiveRoles_ExcludesInactiveRoles()
    {
        // Arrange
        var userPublicId = Guid.NewGuid();
        var user = CreateTestUser(userPublicId, "test@example.com");
        var activeRole = CreateTestRole("Active", "Active role");
        var inactiveRole = CreateTestRole("Inactive", "Inactive role", isActive: false);

        _context.Users.Add(user);
        _context.Roles.AddRange(activeRole, inactiveRole);
        await _context.SaveChangesAsync(CancellationToken.None);

        _context.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = activeRole.Id },
            new UserRole { UserId = user.Id, RoleId = inactiveRole.Id });
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _userQueryService.GetUserRolesByPublicIdAsync(userPublicId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(r => r.Name == "Active");
        result.Should().NotContain(r => r.Name == "Inactive");
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



