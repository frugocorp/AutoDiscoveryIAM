using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public interface ICreateUserRolesService
{
  Task<OperationResult<CreateUserRolesResponseDto>> ExecuteAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken, bool persistChanges = true);
}

public class CreateUserRolesService(
    ICreateUserRolesRepository repository,
    IRestoreUserRolesByPublicIdService restoreUserRolesByPublicIdService,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : ICreateUserRolesService
{
  private readonly ICreateUserRolesRepository _repository = repository;
  private readonly IRestoreUserRolesByPublicIdService _restoreUserRolesByPublicIdService = restoreUserRolesByPublicIdService;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

  public async Task<OperationResult<CreateUserRolesResponseDto>> ExecuteAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken, bool persistChanges = true)
  {
    var validationResult = await ValidateUserAndRoleAsync(userPublicId, rolePublicId, cancellationToken);
    if (!validationResult.Success)
    {
      return OperationResult.Fail<CreateUserRolesResponseDto>(validationResult.ErrorInfo!, validationResult.Error!);
    }

    var (user, role) = validationResult.Data!;
    var relation = await _repository.GetRelationAsync(user.Id, role.Id, cancellationToken);

    if (relation != null && !relation.IsDeleted)
    {
      return OperationResult.Fail<CreateUserRolesResponseDto>(ErrorInfo.Status422Business, string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RelationAlreadyAssigned.Message, userPublicId, rolePublicId));
    }

    if (relation != null && relation.IsDeleted)
    {
      var restoreResult = await _restoreUserRolesByPublicIdService.ExecuteAsync(userPublicId, rolePublicId, cancellationToken, persistChanges);
      return !restoreResult.Success
        ? OperationResult.Fail<CreateUserRolesResponseDto>(restoreResult.ErrorInfo!, restoreResult.Error!)
        : OperationResult.Ok(new CreateUserRolesResponseDto(userPublicId, rolePublicId, restoreResult.Data!.RestoredAt));
    }

    return await CreateNewRelationAsync(user, role, userPublicId, rolePublicId, persistChanges, cancellationToken);
  }

  private async Task<OperationResult<(User user, Role role)>> ValidateUserAndRoleAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken)
  {
    var user = await _repository.GetUserByPublicIdAsync(userPublicId, cancellationToken);
    if (user == null)
    {
      return OperationResult.Fail<(User, Role)>(ErrorInfo.Status404NotFound, string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId));
    }

    if (!user.IsActive || user.IsDeleted)
    {
      return OperationResult.Fail<(User, Role)>(ErrorInfo.Status422Business, string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId));
    }

    var role = await _repository.GetRoleByPublicIdAsync(rolePublicId, cancellationToken);
    if (role == null)
    {
      return OperationResult.Fail<(User, Role)>(ErrorInfo.Status404NotFound, string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
    }

    if (!role.IsActive || role.IsDeleted)
    {
      return OperationResult.Fail<(User, Role)>(ErrorInfo.Status422Business, string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
    }

    return OperationResult.Ok((user, role));
  }

  private async Task<OperationResult<CreateUserRolesResponseDto>> CreateNewRelationAsync(User user, Role role, Guid userPublicId, Guid rolePublicId, bool persistChanges, CancellationToken cancellationToken)
  {
    var now = DateTime.UtcNow;
    await _repository.AddRelationAsync(new UserRole
    {
      UserId = user.Id,
      RoleId = role.Id,
      AssignedAt = now,
      CreatedAt = now,
      CreatedBy = 1,
    }, cancellationToken);

    if (persistChanges)
    {
      await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    return OperationResult.Ok(new CreateUserRolesResponseDto(userPublicId, rolePublicId, now));
  }
}
