using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data;

/// <summary>
/// Regresión issue #076: <c>AuditableEntityConfiguration&lt;T&gt;</c> (generico abierto) nunca se
/// aplicaba via <c>ApplyConfigurationsFromAssembly</c> — todas las entidades auditables salvo
/// User/Role no tenian ningun indice unico sobre PublicId. Este test verifica, a nivel de modelo
/// EF, que cada entidad que hereda de <see cref="AuditableEntity"/> tiene un indice unico cuyas
/// columnas son exactamente [PublicId].
/// </summary>
public class PublicIdUniqueIndexTests
{
  private static AutoDiscoveryIAMDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    return new AutoDiscoveryIAMDbContext(options, Substitute.For<ICurrentAuditUserService>());
  }

  [Fact]
  public void Model_EveryAuditableEntity_ShouldHaveUniqueIndexOnPublicId()
  {
    using var context = CreateContext();

    var auditableEntityTypes = context.Model.GetEntityTypes()
        .Where(et => et.ClrType != null && typeof(AuditableEntity).IsAssignableFrom(et.ClrType))
        .ToList();

    auditableEntityTypes.Should().NotBeEmpty();

    foreach (var entityType in auditableEntityTypes)
    {
      var hasUniquePublicIdIndex = entityType.GetIndexes()
          .Any(index => index.IsUnique
              && index.Properties.Count == 1
              && index.Properties[0].Name == nameof(AuditableEntity.PublicId));

      hasUniquePublicIdIndex.Should().BeTrue(
          $"la entidad '{entityType.ClrType!.Name}' debe tener un indice unico sobre PublicId (issue #076)");
    }
  }
}
