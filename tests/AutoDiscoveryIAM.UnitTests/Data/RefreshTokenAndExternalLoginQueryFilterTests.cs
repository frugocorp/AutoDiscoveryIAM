using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data;

/// <summary>
/// Regresión issue #075: RefreshToken/ExternalLogin heredan IsDeleted de AuditableEntity pero no
/// tenían HasQueryFilter, a diferencia de User/Role/Permission/Post — un registro soft-deleted
/// seguía siendo visible a cualquier consulta normal.
/// </summary>
public class RefreshTokenAndExternalLoginQueryFilterTests : IDisposable
{
    private readonly ICurrentAuditUserService _auditUserService = Substitute.For<ICurrentAuditUserService>();
    private AutoDiscoveryIAMDbContext _dbContext = default!;

    private AutoDiscoveryIAMDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AutoDiscoveryIAMDbContext(options, _auditUserService);
    }

    private static User CreateUser() => new()
    {
        Username = $"user-{Guid.NewGuid():N}",
        Email = $"{Guid.NewGuid():N}@example.com",
        PasswordHash = "hash",
        PublicId = Guid.NewGuid(),
    };

    [Fact]
    public async Task RefreshTokens_ShouldExcludeSoftDeletedRow_FromNormalQuery()
    {
        _auditUserService.GetCurrentUser().Returns(new CurrentAuditUser(1, Guid.NewGuid(), "user@test.com", "Test User", Array.Empty<string>()));
        _dbContext = CreateContext(nameof(RefreshTokens_ShouldExcludeSoftDeletedRow_FromNormalQuery));

        var user = CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var token = new RefreshToken { Token = "hash-token", UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        token.IsDeleted = true;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var visible = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == token.Id, CancellationToken.None);
        var ignoringFilters = await _dbContext.RefreshTokens.IgnoreQueryFilters().FirstOrDefaultAsync(rt => rt.Id == token.Id, CancellationToken.None);

        visible.Should().BeNull();
        ignoringFilters.Should().NotBeNull();
    }

    [Fact]
    public async Task ExternalLogins_ShouldExcludeSoftDeletedRow_FromNormalQuery()
    {
        _auditUserService.GetCurrentUser().Returns(new CurrentAuditUser(1, Guid.NewGuid(), "user@test.com", "Test User", Array.Empty<string>()));
        _dbContext = CreateContext(nameof(ExternalLogins_ShouldExcludeSoftDeletedRow_FromNormalQuery));

        var user = CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var externalLogin = new ExternalLogin { UserId = user.Id, Provider = "Google", ProviderId = "google-subject" };
        _dbContext.ExternalLogins.Add(externalLogin);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        externalLogin.IsDeleted = true;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var visible = await _dbContext.ExternalLogins.FirstOrDefaultAsync(x => x.Id == externalLogin.Id, CancellationToken.None);
        var ignoringFilters = await _dbContext.ExternalLogins.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == externalLogin.Id, CancellationToken.None);

        visible.Should().BeNull();
        ignoringFilters.Should().NotBeNull();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
