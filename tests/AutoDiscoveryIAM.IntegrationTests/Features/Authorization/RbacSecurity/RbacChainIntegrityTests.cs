using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.RbacSecurity;

[Collection("GlobalTestSetup")]
public class RbacChainIntegrityTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task HasPermissionByPublicIdAsync_WithCompleteActiveChain_ShouldReturnTrue()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, permission) = await SeedPermissionChainAsync(db, userRoleDeleted: false, rolePermissionDeleted: false, roleIsActive: true, permissionIsActive: true);

        var hasPermission = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);

        hasPermission.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenUserRoleSoftDeleted_ShouldReturnFalse()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, permission) = await SeedPermissionChainAsync(db, userRoleDeleted: false, rolePermissionDeleted: false, roleIsActive: true, permissionIsActive: true);

        var userRole = await db.UserRoles.FirstAsync(ur => ur.UserId == user.Id, CancellationToken.None);
        userRole.IsDeleted = true;
        userRole.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);

        var hasPermission = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);

        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenRolePermissionSoftDeleted_ShouldReturnFalse()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, permission) = await SeedPermissionChainAsync(db, userRoleDeleted: false, rolePermissionDeleted: false, roleIsActive: true, permissionIsActive: true);

        var rolePermission = await db.RolePermissions.FirstAsync(rp => rp.PermissionId == permission.Id, CancellationToken.None);
        rolePermission.IsDeleted = true;
        rolePermission.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);

        var hasPermission = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);

        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenRoleInactive_ShouldReturnFalse()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, permission) = await SeedPermissionChainAsync(db, userRoleDeleted: false, rolePermissionDeleted: false, roleIsActive: false, permissionIsActive: true);

        var hasPermission = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);

        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenPermissionInactive_ShouldReturnFalse()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, permission) = await SeedPermissionChainAsync(db, userRoleDeleted: false, rolePermissionDeleted: false, roleIsActive: true, permissionIsActive: false);

        var hasPermission = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);

        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WithMultipleRoles_WhenOneRoleHasPermission_ShouldReturnTrue()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var user = new User
        {
            PublicId = Guid.NewGuid(),
            Username = $"users-query-user-{Guid.NewGuid():N}",
            Email = $"users-query-user-{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var deniedRole = new Role
        {
            PublicId = Guid.NewGuid(),
            Name = $"roles-query-perm-role-{Guid.NewGuid():N}",
            Description = "role without permission",
            IsActive = true,
            IsDeleted = false,
        };

        var grantedRole = new Role
        {
            PublicId = Guid.NewGuid(),
            Name = $"roles-query-perm-role-{Guid.NewGuid():N}",
            Description = "role with permission",
            IsActive = true,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            PublicId = Guid.NewGuid(),
            Key = $"roles.query.permissions.{Guid.NewGuid():N}",
            Description = "permission para chain test",
            Group = "roles",
            IsActive = true,
            IsDeleted = false,
        };

        db.Users.Add(user);
        db.Roles.AddRange(deniedRole, grantedRole);
        db.Permissions.Add(permission);
        await db.SaveChangesAsync(CancellationToken.None);

        db.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = deniedRole.Id, User = user, Role = deniedRole, IsDeleted = false },
            new UserRole { UserId = user.Id, RoleId = grantedRole.Id, User = user, Role = grantedRole, IsDeleted = false });

        db.RolePermissions.Add(new RolePermission
        {
            RoleId = grantedRole.Id,
            PermissionId = permission.Id,
            Role = grantedRole,
            Permission = permission,
            IsDeleted = false,
        });

        await db.SaveChangesAsync(CancellationToken.None);

        var hasPermission = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);

        hasPermission.Should().BeTrue();
    }

    private static async Task<(User User, Permission Permission)> SeedPermissionChainAsync(
        AutoDiscoveryIAMDbContext db,
        bool userRoleDeleted,
        bool rolePermissionDeleted,
        bool roleIsActive,
        bool permissionIsActive)
    {
        var user = new User
        {
            PublicId = Guid.NewGuid(),
            Username = $"users-query-user-{Guid.NewGuid():N}",
            Email = $"users-query-user-{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var role = new Role
        {
            PublicId = Guid.NewGuid(),
            Name = $"roles-query-perm-role-{Guid.NewGuid():N}",
            Description = "role para chain test",
            IsActive = roleIsActive,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            PublicId = Guid.NewGuid(),
            Key = $"roles.query.permissions.{Guid.NewGuid():N}",
            Description = "permission para chain test",
            Group = "roles",
            IsActive = permissionIsActive,
            IsDeleted = false,
        };

        db.Users.Add(user);
        db.Roles.Add(role);
        db.Permissions.Add(permission);
        await db.SaveChangesAsync(CancellationToken.None);

        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role,
            IsDeleted = userRoleDeleted,
        });

        db.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            Role = role,
            Permission = permission,
            IsDeleted = rolePermissionDeleted,
        });

        await db.SaveChangesAsync(CancellationToken.None);

        return (user, permission);
    }
}
