using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.RbacSecurity;

[Collection("GlobalTestSetup")]
public class SoftDeleteSecurityTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenRoleSoftDeletedAfterGrant_ShouldBlockAccess()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, role, permission) = await SeedActiveChainAsync(db);

        var beforeDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        beforeDelete.Should().BeTrue();

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);

        var afterDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        afterDelete.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenUserRoleSoftDeletedAfterGrant_ShouldBlockAccess()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, _, permission) = await SeedActiveChainAsync(db);

        var beforeDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        beforeDelete.Should().BeTrue();

        var userRole = await db.UserRoles.FirstAsync(ur => ur.UserId == user.Id, CancellationToken.None);
        userRole.IsDeleted = true;
        userRole.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);

        var afterDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        afterDelete.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenRolePermissionSoftDeletedAfterGrant_ShouldBlockAccess()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, _, permission) = await SeedActiveChainAsync(db);

        var beforeDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        beforeDelete.Should().BeTrue();

        var rolePermission = await db.RolePermissions.FirstAsync(rp => rp.PermissionId == permission.Id, CancellationToken.None);
        rolePermission.IsDeleted = true;
        rolePermission.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);

        var afterDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        afterDelete.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionByPublicIdAsync_WhenUserSoftDeleted_ShouldBlockAccess()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();

        var (user, _, permission) = await SeedActiveChainAsync(db);

        var beforeDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        beforeDelete.Should().BeTrue();

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);

        var afterDelete = await userQueryService.HasPermissionByPublicIdAsync(user.PublicId, permission.Key);
        afterDelete.Should().BeFalse();
    }

    private static async Task<(User User, Role Role, Permission Permission)> SeedActiveChainAsync(AutoDiscoveryIAMDbContext db)
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
            Description = "role para soft-delete tests",
            IsActive = true,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            PublicId = Guid.NewGuid(),
            Key = $"roles.query.permissions.{Guid.NewGuid():N}",
            Description = "permission para soft-delete tests",
            Group = "roles",
            IsActive = true,
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
            IsDeleted = false,
        });

        db.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            Role = role,
            Permission = permission,
            IsDeleted = false,
        });

        await db.SaveChangesAsync(CancellationToken.None);

        return (user, role, permission);
    }
}
