using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.DeleteUserRolesByPublicId;

public class DeleteUserRolesByPublicIdServiceTests
{
    private readonly IDeleteUserRolesByPublicIdRepository _repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
    private readonly DeleteUserRolesByPublicIdService _service;

    public DeleteUserRolesByPublicIdServiceTests()
    {
        _repository = Substitute.For<IDeleteUserRolesByPublicIdRepository>();
        _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        _service = new DeleteUserRolesByPublicIdService(_repository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRelationDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns((UserRole?)null);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.ActiveRelationNotFound.Message, userPublicId, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserOrRoleIsInactive_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = BuildRelation(userPublicId, rolePublicId, userActive: false, roleActive: true, isDeleted: false);

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(UserRoleAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRelationIsActive_ShouldSoftDeleteAndPersist()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = BuildRelation(userPublicId, rolePublicId, userActive: true, roleActive: true, isDeleted: false);

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        relation.IsDeleted.Should().BeTrue();
        relation.DeletedAt.Should().NotBeNull();
        relation.DeletedBy.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsDeleted_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = new UserRole
        {
            UserId = 11,
            RoleId = 21,
            IsDeleted = false,
            User = new User
            {
                Id = 11,
                PublicId = userPublicId,
                Username = "user-deleted",
                Email = "user-deleted@example.com",
                PasswordHash = "hash",
                Status = "active",
                IsActive = true,
                IsDeleted = true,
            },
            Role = new Role
            {
                Id = 21,
                PublicId = rolePublicId,
                Name = "role-ok",
                IsActive = true,
                IsDeleted = false,
            },
        };

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(UserRoleAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsDeleted_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = new UserRole
        {
            UserId = 12,
            RoleId = 22,
            IsDeleted = false,
            User = new User
            {
                Id = 12,
                PublicId = userPublicId,
                Username = "user-ok",
                Email = "user-ok@example.com",
                PasswordHash = "hash",
                Status = "active",
                IsActive = true,
                IsDeleted = false,
            },
            Role = new Role
            {
                Id = 22,
                PublicId = rolePublicId,
                Name = "role-deleted",
                IsActive = true,
                IsDeleted = true,
            },
        };

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(UserRoleAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBothUserAndRoleAreInactive_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = new UserRole
        {
            UserId = 13,
            RoleId = 23,
            IsDeleted = false,
            User = new User
            {
                Id = 13,
                PublicId = userPublicId,
                Username = "user-both-inactive",
                Email = "user-both-inactive@example.com",
                PasswordHash = "hash",
                Status = "inactive",
                IsActive = false,
                IsDeleted = false,
            },
            Role = new Role
            {
                Id = 23,
                PublicId = rolePublicId,
                Name = "role-both-inactive",
                IsActive = false,
                IsDeleted = false,
            },
        };

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(UserRoleAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersistChangesIsFalse_ShouldNotSaveChanges()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = BuildRelation(userPublicId, rolePublicId, userActive: true, roleActive: true, isDeleted: false);

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None, persistChanges: false);

        result.Success.Should().BeTrue();
        relation.IsDeleted.Should().BeTrue();
        relation.DeletedBy.Should().Be(1);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAssignDeletedAtDeletedByAndUpdateTimestamps()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var beforeExecution = DateTime.UtcNow;
        var relation = new UserRole
        {
            UserId = 14,
            RoleId = 24,
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null,
            UpdatedAt = beforeExecution.AddMinutes(-5),
            UpdatedBy = 1,
            AssignedAt = beforeExecution.AddMinutes(-10),
            User = new User
            {
                Id = 14,
                PublicId = userPublicId,
                Username = "user-timestamps",
                Email = "user-timestamps@example.com",
                PasswordHash = "hash",
                Status = "active",
                IsActive = true,
                IsDeleted = false,
            },
            Role = new Role
            {
                Id = 24,
                PublicId = rolePublicId,
                Name = "role-timestamps",
                IsActive = true,
                IsDeleted = false,
            },
        };

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        relation.IsDeleted.Should().BeTrue();
        relation.DeletedAt.Should().NotBeNull();
        relation.DeletedAt.Should().BeGreaterThanOrEqualTo(beforeExecution);
        relation.DeletedBy.Should().Be(1);
        relation.UpdatedAt.Should().BeGreaterThanOrEqualTo(beforeExecution);
        relation.UpdatedBy.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResponseDtoWithCorrectPublicIds()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = BuildRelation(userPublicId, rolePublicId, userActive: true, roleActive: true, isDeleted: false);

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserPublicId.Should().Be(userPublicId);
        result.Data.RolePublicId.Should().Be(rolePublicId);
        result.Data.UnassignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactiveButNotDeleted_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var relation = new UserRole
        {
            UserId = 15,
            RoleId = 25,
            IsDeleted = false,
            User = new User
            {
                Id = 15,
                PublicId = userPublicId,
                Username = "user-inactive-not-deleted",
                Email = "user-inactive-not-deleted@example.com",
                PasswordHash = "hash",
                Status = "inactive",
                IsActive = false,
                IsDeleted = false,
            },
            Role = new Role
            {
                Id = 25,
                PublicId = rolePublicId,
                Name = "role-ok-2",
                IsActive = true,
                IsDeleted = false,
            },
        };

        _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(UserRoleAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static UserRole BuildRelation(Guid userPublicId, Guid rolePublicId, bool userActive, bool roleActive, bool isDeleted)
        => new()
        {
            UserId = 10,
            RoleId = 20,
            IsDeleted = isDeleted,
            User = new User
            {
                Id = 10,
                PublicId = userPublicId,
                Username = "user-test",
                Email = "user-test@example.com",
                PasswordHash = "hash",
                Status = "active",
                IsActive = userActive,
                IsDeleted = !userActive,
            },
            Role = new Role
            {
                Id = 20,
                PublicId = rolePublicId,
                Name = "role-test",
                IsActive = roleActive,
                IsDeleted = !roleActive,
            },
        };
}

