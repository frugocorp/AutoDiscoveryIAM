using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public class DeleteRolePermissionsByPermissionPublicIdServiceTests
{
  private readonly IDeleteRolePermissionsByPermissionPublicIdRepository _repository;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
  private readonly DeleteRolePermissionsByPermissionPublicIdService _service;

  public DeleteRolePermissionsByPermissionPublicIdServiceTests()
  {
    _repository = Substitute.For<IDeleteRolePermissionsByPermissionPublicIdRepository>();
    _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    _service = new DeleteRolePermissionsByPermissionPublicIdService(_repository, _unitOfWork);
  }

  [Fact]
  public async Task ExecuteAsync_WhenRelationDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns((RolePermission?)null);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.ActiveRelationNotFound.Message, rolePublicId, permissionPublicId));
    await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenRoleOrPermissionIsInactive_ShouldReturnBusinessError()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: false, permissionActive: true, isDeleted: false);

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns(relation);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
    await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenRelationIsActive_ShouldSoftDeleteAndPersist()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: true, isDeleted: false);

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns(relation);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

    result.Success.Should().BeTrue();
    relation.IsDeleted.Should().BeTrue();
    relation.DeletedAt.Should().NotBeNull();
    relation.DeletedBy.Should().Be(1);
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task ExecuteAsync_WhenRelationIsAlreadyDeleted_ShouldReturnNotFound()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: true, isDeleted: true);

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns(relation);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.ActiveRelationNotFound.Message, rolePublicId, permissionPublicId));
    await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenPermissionIsInactiveButRoleActive_ShouldReturnBusinessError()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: false, isDeleted: false);

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns(relation);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be(RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
    await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenPersistChangesIsFalse_ShouldNotSaveChanges()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: true, isDeleted: false);

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns(relation);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None, persistChanges: false);

    result.Success.Should().BeTrue();
    relation.IsDeleted.Should().BeTrue();
    relation.DeletedBy.Should().Be(1);
    await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
  }

  [Fact]
  public async Task ExecuteAsync_ShouldAssignDeletedAtDeletedByAndUpdateTimestamps()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var beforeExecution = DateTime.UtcNow;
    var relation = new RolePermission
    {
      RoleId = 10,
      PermissionId = 20,
      IsDeleted = false,
      DeletedAt = null,
      DeletedBy = null,
      UpdatedAt = beforeExecution.AddMinutes(-5),
      UpdatedBy = 1,
      Role = new Role
      {
        Id = 10,
        PublicId = rolePublicId,
        Name = "role-timestamps",
        IsActive = true,
        IsDeleted = false,
      },
      Permission = new Permission
      {
        Id = 20,
        PublicId = permissionPublicId,
        Key = "permissions.test-timestamps",
        IsActive = true,
        IsDeleted = false,
      },
    };

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns(relation);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

    result.Success.Should().BeTrue();
    relation.IsDeleted.Should().BeTrue();
    relation.DeletedAt.Should().NotBeNull();
    relation.DeletedAt!.Value.Should().BeGreaterThanOrEqualTo(beforeExecution);
    relation.DeletedBy.Should().Be(1);
    relation.UpdatedAt.Should().BeGreaterThanOrEqualTo(beforeExecution);
    relation.UpdatedBy.Should().Be(1);
  }

  [Fact]
  public async Task ExecuteAsync_ShouldReturnResponseDtoWithCorrectPublicIds()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: true, isDeleted: false);

    _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
        .Returns(relation);

    var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.RolePublicId.Should().Be(rolePublicId);
    result.Data.PermissionPublicId.Should().Be(permissionPublicId);
    result.Data.UnassignedAt.Should().NotBeNull();
  }

  private static RolePermission BuildRelation(Guid rolePublicId, Guid permissionPublicId, bool roleActive, bool permissionActive, bool isDeleted)
      => new()
      {
        RoleId = 10,
        PermissionId = 20,
        IsDeleted = isDeleted,
        Role = new Role
        {
          Id = 10,
          PublicId = rolePublicId,
          Name = "role-test",
          IsActive = roleActive,
          IsDeleted = !roleActive,
        },
        Permission = new Permission
        {
          Id = 20,
          PublicId = permissionPublicId,
          Key = $"permissions.{Guid.NewGuid():N}",
          IsActive = permissionActive,
          IsDeleted = !permissionActive,
        },
      };
}




