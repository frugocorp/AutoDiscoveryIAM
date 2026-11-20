using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUserRoles;

public class CreateUserRolesServiceTests
{
    private readonly ICreateUserRolesRepository _repository;
    private readonly IRestoreUserRolesByPublicIdService _restoreService;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
    private readonly CreateUserRolesService _service;

    public CreateUserRolesServiceTests()
    {
        _repository = Substitute.For<ICreateUserRolesRepository>();
        _restoreService = Substitute.For<IRestoreUserRolesByPublicIdService>();
        _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();

        _service = new CreateUserRolesService(_repository, _restoreService, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>())
          .Returns((User?)null);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        var inactiveUser = new User
        {
            Id = 10,
            PublicId = userPublicId,
            Username = "inactive-user",
            Email = "inactive-user@example.com",
            PasswordHash = "hash",
            IsActive = false,
            IsDeleted = false,
            Status = "inactive",
        };

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>())
          .Returns(inactiveUser);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsDeleted_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        var deletedUser = new User
        {
            Id = 11,
            PublicId = userPublicId,
            Username = "deleted-user",
            Email = "deleted-user@example.com",
            PasswordHash = "hash",
            IsActive = true,
            IsDeleted = true,
            Status = "active",
        };

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>())
          .Returns(deletedUser);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var user = BuildUser(userPublicId, isActive: true, isDeleted: false);

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsInactive_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var user = BuildUser(userPublicId, isActive: true, isDeleted: false);
        var role = BuildRole(rolePublicId, isActive: false, isDeleted: false);

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsDeleted_ShouldReturnBusinessError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var user = BuildUser(userPublicId, isActive: true, isDeleted: false);
        var role = BuildRole(rolePublicId, isActive: true, isDeleted: true);

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRelationAlreadyExists_ShouldReturnBusinessErrorWithCentralizedMessage()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        var user = new User
        {
            Id = 1,
            PublicId = userPublicId,
            Username = "test-user",
            Email = "test-user@example.com",
            PasswordHash = "hash",
            IsActive = true,
            IsDeleted = false,
            Status = "active",
        };

        var role = new Role
        {
            Id = 2,
            PublicId = rolePublicId,
            Name = "test-role",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetRelationAsync(user.Id, role.Id, Arg.Any<CancellationToken>())
          .Returns(new UserRole { UserId = user.Id, RoleId = role.Id, IsDeleted = false });

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RelationAlreadyAssigned.Message, userPublicId, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDeletedRelationExists_ShouldRestoreAndReturnSuccess()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var user = BuildUser(userPublicId, isActive: true, isDeleted: false);
        var role = BuildRole(rolePublicId, isActive: true, isDeleted: false);
        var restoredAt = DateTime.UtcNow;

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetRelationAsync(user.Id, role.Id, Arg.Any<CancellationToken>())
            .Returns(new UserRole { UserId = user.Id, RoleId = role.Id, IsDeleted = true });

        _restoreService.ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>(), true)
            .Returns(OperationResult.Ok(new RestoreUserRolesByPublicIdResponseDto(userPublicId, rolePublicId, restoredAt)));

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None, persistChanges: true);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserPublicId.Should().Be(userPublicId);
        result.Data.RolePublicId.Should().Be(rolePublicId);
        result.Data.AssignedAt.Should().Be(restoredAt);
        await _restoreService.Received(1).ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>(), true);
        await _repository.DidNotReceiveWithAnyArgs().AddRelationAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRestoreFails_ShouldPropagateError()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var user = BuildUser(userPublicId, isActive: true, isDeleted: false);
        var role = BuildRole(rolePublicId, isActive: true, isDeleted: false);

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetRelationAsync(user.Id, role.Id, Arg.Any<CancellationToken>())
            .Returns(new UserRole { UserId = user.Id, RoleId = role.Id, IsDeleted = true });

        _restoreService.ExecuteAsync(userPublicId, rolePublicId, Arg.Any<CancellationToken>(), false)
            .Returns(OperationResult.Fail<RestoreUserRolesByPublicIdResponseDto>(ErrorInfo.Status422Business, "restore failed"));

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None, persistChanges: false);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be("restore failed");
        await _repository.DidNotReceiveWithAnyArgs().AddRelationAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataIsValid_ShouldCreateRelationAndPersist()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();

        var user = new User
        {
            Id = 10,
            PublicId = userPublicId,
            Username = "ok-user",
            Email = "ok-user@example.com",
            PasswordHash = "hash",
            IsActive = true,
            IsDeleted = false,
            Status = "active",
        };

        var role = new Role
        {
            Id = 20,
            PublicId = rolePublicId,
            Name = "ok-role",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetRelationAsync(user.Id, role.Id, Arg.Any<CancellationToken>()).Returns((UserRole?)null);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserPublicId.Should().Be(userPublicId);
        result.Data.RolePublicId.Should().Be(rolePublicId);
        await _repository.Received(1).AddRelationAsync(Arg.Is<UserRole>(relation => relation.UserId == user.Id && relation.RoleId == role.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersistChangesIsFalse_ShouldCreateRelationWithoutSaving()
    {
        var userPublicId = Guid.NewGuid();
        var rolePublicId = Guid.NewGuid();
        var user = BuildUser(userPublicId, isActive: true, isDeleted: false);
        var role = BuildRole(rolePublicId, isActive: true, isDeleted: false);

        _repository.GetUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetRelationAsync(user.Id, role.Id, Arg.Any<CancellationToken>()).Returns((UserRole?)null);

        var result = await _service.ExecuteAsync(userPublicId, rolePublicId, CancellationToken.None, persistChanges: false);

        result.Success.Should().BeTrue();
        await _repository.Received(1).AddRelationAsync(Arg.Is<UserRole>(relation => relation.UserId == user.Id && relation.RoleId == role.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static User BuildUser(Guid publicId, bool isActive, bool isDeleted)
      => new()
      {
          Id = 100,
          PublicId = publicId,
          Username = "user-test",
          Email = "user-test@example.com",
          PasswordHash = "hash",
          IsActive = isActive,
          IsDeleted = isDeleted,
          Status = "active",
      };

    private static Role BuildRole(Guid publicId, bool isActive, bool isDeleted)
      => new()
      {
          Id = 200,
          PublicId = publicId,
          Name = "role-test",
          IsActive = isActive,
          IsDeleted = isDeleted,
      };
}



