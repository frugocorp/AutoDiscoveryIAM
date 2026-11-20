using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data;

/// <summary>
/// Tests unitarios para verificar que AutoDiscoveryIAMDbContext.ApplyAuditInfo() rellena correctamente
/// los campos de auditoría (CreatedBy, UpdatedBy, DeletedBy, RestoredBy) en todas las operaciones.
/// </summary>
public class AutoDiscoveryIAMDbContextAuditTests : IDisposable
{
    private readonly ICurrentAuditUserService _auditUserService;
    private AutoDiscoveryIAMDbContext _dbContext = default!;

    public AutoDiscoveryIAMDbContextAuditTests()
    {
        _auditUserService = Substitute.For<ICurrentAuditUserService>();
    }

    private AutoDiscoveryIAMDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AutoDiscoveryIAMDbContext(options, _auditUserService);
    }

    private static CurrentAuditUser UserWith(long id) =>
        new(id, Guid.NewGuid(), "user@test.com", "Test User", Array.Empty<string>());

    private static CurrentAuditUser AnonymousUser() =>
        new(null, null, null, null, Array.Empty<string>());

    // ───────────────────────────────────────────────────────────────────────
    // ADDED
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedAt()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(10));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityAdded_SetsCreatedAt));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedByToAuthenticatedUser()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(42));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityAdded_SetsCreatedByToAuthenticatedUser));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.CreatedBy.Should().Be(42);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsIsDeletedToFalse()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(10));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityAdded_SetsIsDeletedToFalse));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_DoesNotSetUpdatedAtOrUpdatedBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(10));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityAdded_DoesNotSetUpdatedAtOrUpdatedBy));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.UpdatedAt.Should().BeNull();
        role.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAddedWithCreatedByPreset_DoesNotOverwriteCreatedBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(99));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityAddedWithCreatedByPreset_DoesNotOverwriteCreatedBy));

        var role = new Role { Name = "T", CreatedBy = 7 };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.CreatedBy.Should().Be(7);
    }

    // ───────────────────────────────────────────────────────────────────────
    // MODIFIED (normal)
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_SetsUpdatedAtAndUpdatedBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(55));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityModified_SetsUpdatedAtAndUpdatedBy));

        var role = new Role { Name = "Original" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.Name = "Modified";
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.UpdatedAt.Should().NotBeNull();
        role.UpdatedBy.Should().Be(55);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_DoesNotChangeCreatedAtOrCreatedBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(55));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityModified_DoesNotChangeCreatedAtOrCreatedBy));

        var role = new Role { Name = "Original" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var originalCreatedAt = role.CreatedAt;
        var originalCreatedBy = role.CreatedBy;

        role.Name = "Modified";
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.CreatedAt.Should().Be(originalCreatedAt);
        role.CreatedBy.Should().Be(originalCreatedBy);
    }

    // ───────────────────────────────────────────────────────────────────────
    // DELETED (soft-delete)
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenEntityDeleted_SetsIsDeletedAndDeletedAtAndDeletedBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(77));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityDeleted_SetsIsDeletedAndDeletedAtAndDeletedBy));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.IsDeleted.Should().BeTrue();
        role.DeletedAt.Should().NotBeNull();
        role.DeletedBy.Should().Be(77);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityDeleted_RecordStillExistsInDb_SoftDelete()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(77));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityDeleted_RecordStillExistsInDb_SoftDelete));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var savedId = role.Id;
        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var inDb = await _dbContext.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == savedId, CancellationToken.None);
        inDb.Should().NotBeNull();
        inDb!.IsDeleted.Should().BeTrue();
    }

    // ───────────────────────────────────────────────────────────────────────
    // RESTORED
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenEntityRestored_SetsRestoredAtAndRestoredBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(88));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityRestored_SetsRestoredAtAndRestoredBy));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.IsDeleted = false;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.RestoredAt.Should().NotBeNull();
        role.RestoredBy.Should().Be(88);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityRestored_DoesNotSetUpdatedAtOrUpdatedBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(88));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityRestored_DoesNotSetUpdatedAtOrUpdatedBy));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var updatedAtBeforeRestore = role.UpdatedAt;
        var updatedByBeforeRestore = role.UpdatedBy;

        role.IsDeleted = false;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.UpdatedAt.Should().Be(updatedAtBeforeRestore);
        role.UpdatedBy.Should().Be(updatedByBeforeRestore);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityRestored_PreservesDeletedAtAndDeletedBy()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(88));
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenEntityRestored_PreservesDeletedAtAndDeletedBy));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var deletedAt = role.DeletedAt;
        var deletedBy = role.DeletedBy;

        role.IsDeleted = false;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.DeletedAt.Should().Be(deletedAt);
        role.DeletedBy.Should().Be(deletedBy);
    }

    // ───────────────────────────────────────────────────────────────────────
    // FALLBACK - usuario anónimo
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenNoUserAuthenticated_FallsBackToSystemUser()
    {
        _auditUserService.GetCurrentUser().Returns(AnonymousUser());
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenNoUserAuthenticated_FallsBackToSystemUser));

        // Issue #90: el fallback ya no asume Id=1 fijo (ese truco requería IDENTITY_INSERT,
        // específico de SQL Server) -- ahora resuelve el usuario "system" por nombre, así que hay
        // que sembrarlo primero (en su propio SaveChanges, para que ya esté persistido y sea
        // encontrable por la consulta del fallback antes de guardar el Role).
        var systemUser = new User { Username = AutoDiscoveryIAMDbContext.SystemUsername };
        _dbContext.Users.Add(systemUser);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.CreatedBy.Should().Be(systemUser.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoUserAuthenticated_SoftDeleteFallsBackToSystemUser()
    {
        _auditUserService.GetCurrentUser().Returns(AnonymousUser());
        _dbContext = CreateContext(nameof(SaveChangesAsync_WhenNoUserAuthenticated_SoftDeleteFallsBackToSystemUser));

        var systemUser = new User { Username = AutoDiscoveryIAMDbContext.SystemUsername };
        _dbContext.Users.Add(systemUser);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        role.DeletedBy.Should().Be(systemUser.Id);
    }

    // ───────────────────────────────────────────────────────────────────────
    // SaveChanges (sync) - debe ser equivalente a SaveChangesAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void SaveChanges_WhenEntityAdded_SetsCreatedByToAuthenticatedUser()
    {
        _auditUserService.GetCurrentUser().Returns(UserWith(33));
        _dbContext = CreateContext(nameof(SaveChanges_WhenEntityAdded_SetsCreatedByToAuthenticatedUser));

        var role = new Role { Name = "T" };
        _dbContext.Roles.Add(role);
        _dbContext.SaveChanges();

        role.CreatedBy.Should().Be(33);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
