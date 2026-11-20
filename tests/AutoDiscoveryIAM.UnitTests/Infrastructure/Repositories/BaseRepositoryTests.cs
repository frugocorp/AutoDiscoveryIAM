using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Repositories;

public class BaseRepositoryTests
{
  [Fact]
  public async Task RemoveAsyncById_ShouldReturnTrue_WhenEntityExists()
  {
    await using var context = CreateContext();
    context.Set<TestEntity>().Add(new TestEntity { Id = 1, Name = "one" });
    await context.SaveChangesAsync(CancellationToken.None);

    var sut = new TestRepository(context);

    var removed = await sut.RemoveAsync(1, CancellationToken.None);

    removed.Should().BeTrue();
    sut.Stats.Deleted.Should().Be(1);
  }

  [Fact]
  public async Task RemoveAsyncById_ShouldReturnFalse_WhenEntityDoesNotExist()
  {
    await using var context = CreateContext();
    var sut = new TestRepository(context);

    var removed = await sut.RemoveAsync(999, CancellationToken.None);

    removed.Should().BeFalse();
    sut.Stats.Deleted.Should().Be(0);
  }

  [Fact]
  public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
  {
    await using var context = CreateContext();
    context.Set<TestEntity>().Add(new TestEntity { Id = 10, Name = "exists" });
    await context.SaveChangesAsync(CancellationToken.None);

    var sut = new TestRepository(context);

    var exists = await sut.ExistsAsync(10, CancellationToken.None);

    exists.Should().BeTrue();
    sut.Stats.QueriesExecuted.Should().Be(1);
  }

  [Fact]
  public async Task ExistsAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
  {
    await using var context = CreateContext();
    var sut = new TestRepository(context);

    var exists = await sut.ExistsAsync(11, CancellationToken.None);

    exists.Should().BeFalse();
    sut.Stats.QueriesExecuted.Should().Be(1);
  }

  [Fact]
  public async Task AddAndUpdate_ShouldIncreaseStats()
  {
    await using var context = CreateContext();
    var sut = new TestRepository(context);

    var entity = sut.Add(new TestEntity { Id = 21, Name = "alpha" });
    await context.SaveChangesAsync(CancellationToken.None);

    entity.Name = "beta";
    sut.Update(entity);
    await context.SaveChangesAsync(CancellationToken.None);

    sut.Stats.Inserted.Should().Be(1);
    sut.Stats.Updated.Should().Be(1);
  }

  [Fact]
  public void Add_ShouldThrow_WhenEntityIsNull()
  {
    using var context = CreateContext();
    var sut = new TestRepository(context);

    var act = () => sut.Add(null!);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public async Task AddAsync_ShouldThrow_WhenEntityIsNull()
  {
    await using var context = CreateContext();
    var sut = new TestRepository(context);

    Func<Task> act = async () => await sut.AddAsync(null!, CancellationToken.None);

    await Assert.ThrowsAsync<ArgumentNullException>(act);
  }

  [Fact]
  public void Update_ShouldThrow_WhenEntityIsNull()
  {
    using var context = CreateContext();
    var sut = new TestRepository(context);

    var act = () => sut.Update(null!);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public async Task UpdateAsync_ShouldThrow_WhenEntityIsNull()
  {
    await using var context = CreateContext();
    var sut = new TestRepository(context);

    Func<Task> act = async () => await sut.UpdateAsync(null!, CancellationToken.None);

    await Assert.ThrowsAsync<ArgumentNullException>(act);
  }

  [Fact]
  public async Task RemoveAsync_Entity_ShouldThrow_WhenEntityIsNull()
  {
    await using var context = CreateContext();
    var sut = new TestRepository(context);

    Func<Task> act = async () => await sut.RemoveAsync(null!, CancellationToken.None);

    await Assert.ThrowsAsync<ArgumentNullException>(act);
  }

  [Fact]
  public void Remove_ShouldThrow_WhenEntityIsNull()
  {
    using var context = CreateContext();
    var sut = new TestRepository(context);

    var act = () => sut.Remove(null!);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnEntity_WhenExists()
  {
    await using var context = CreateContext();
    context.Set<TestEntity>().Add(new TestEntity { Id = 50, Name = "found" });
    await context.SaveChangesAsync(CancellationToken.None);
    var sut = new TestRepository(context);

    var entity = await sut.GetByIdAsync(50, CancellationToken.None);

    entity.Should().NotBeNull();
    entity!.Name.Should().Be("found");
    sut.Stats.QueriesExecuted.Should().Be(1);
  }

  [Fact]
  public async Task GetAllAsync_ShouldReturnAllEntities()
  {
    await using var context = CreateContext();
    context.Set<TestEntity>().AddRange(
      new TestEntity { Id = 60, Name = "a" },
      new TestEntity { Id = 61, Name = "b" });
    await context.SaveChangesAsync(CancellationToken.None);
    var sut = new TestRepository(context);

    var list = await sut.GetAllAsync(CancellationToken.None);

    list.Should().HaveCount(2);
    sut.Stats.QueriesExecuted.Should().Be(1);
  }

  private static TestAutoDiscoveryIAMDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
      .Options;

    return new TestAutoDiscoveryIAMDbContext(options);
  }

  private sealed class TestRepository(AutoDiscoveryIAMDbContext context) : BaseRepository<TestEntity>(context)
  {
  }

  private sealed class TestAutoDiscoveryIAMDbContext(DbContextOptions<AutoDiscoveryIAMDbContext> options)
    : AutoDiscoveryIAMDbContext(options)
  {
    public DbSet<TestEntity> TestEntities { get; set; }
  }

  private sealed class TestEntity
  {
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
  }
}
