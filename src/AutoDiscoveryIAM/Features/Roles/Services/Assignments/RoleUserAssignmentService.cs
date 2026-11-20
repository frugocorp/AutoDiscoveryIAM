using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using System.Globalization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;

public record RoleUserAssignmentBulkResult(
    Guid OwnerPublicId,
    bool DryRun,
    int TotalRequested,
    int TotalAssignedNew,
    int TotalAssignedReactivated,
    int TotalUnassigned,
    int TotalUnchanged,
    DateTime ExecutedAt,
    IReadOnlyList<Guid> RequestedPublicIds,
    IReadOnlyList<Guid> AssignedNewPublicIds,
    IReadOnlyList<Guid> AssignedReactivatedPublicIds,
    IReadOnlyList<Guid> UnassignedPublicIds,
    IReadOnlyList<Guid> UnchangedPublicIds);

public interface IRoleUserAssignmentService
{
    Task<OperationResult<RoleUserAssignmentBulkResult>> UpdateUserRolesAsync(Guid userPublicId, IReadOnlyCollection<Guid> rolePublicIds, bool dryRun, CancellationToken cancellationToken);
    Task<OperationResult<RoleUserAssignmentBulkResult>> UpdateRoleUsersAsync(Guid rolePublicId, IReadOnlyCollection<Guid> userPublicIds, bool dryRun, CancellationToken cancellationToken);
}

public class RoleUserAssignmentService(
    IRoleUserAssignmentRepository repository,
    ICreateUserRolesService createUserRolesService,
    IDeleteUserRolesByPublicIdService deleteUserRolesByPublicIdService,
    IRestoreUserRolesByPublicIdService restoreUserRolesByPublicIdService,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : IRoleUserAssignmentService
{
    private readonly IRoleUserAssignmentRepository _repository = repository;
    private readonly ICreateUserRolesService _createUserRolesService = createUserRolesService;
    private readonly IDeleteUserRolesByPublicIdService _deleteUserRolesByPublicIdService = deleteUserRolesByPublicIdService;
    private readonly IRestoreUserRolesByPublicIdService _restoreUserRolesByPublicIdService = restoreUserRolesByPublicIdService;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<RoleUserAssignmentBulkResult>> UpdateUserRolesAsync(Guid userPublicId, IReadOnlyCollection<Guid> rolePublicIds, bool dryRun, CancellationToken cancellationToken)
    {
        var user = await _repository.GetActiveUserByPublicIdAsync(userPublicId, cancellationToken);
        if (user == null)
        {
            return OperationResult.Fail<RoleUserAssignmentBulkResult>(
                ErrorInfo.Status404NotFound,
                string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.ActiveUserNotFound.Message, userPublicId));
        }

        var contextResult = await BuildUserRoleContextAsync(user.Id, rolePublicIds, cancellationToken);
        if (!contextResult.Success)
        {
            return OperationResult.Fail<RoleUserAssignmentBulkResult>(contextResult.ErrorInfo!, contextResult.Error!);
        }

        var context = contextResult.Data!;
        if (!dryRun)
        {
            var execResult = await ExecuteUserRoleChangesAsync(userPublicId, context, cancellationToken);
            if (!execResult.Success)
            {
                return OperationResult.Fail<RoleUserAssignmentBulkResult>(execResult.ErrorInfo!, execResult.Error!);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Ok(BuildBulkResult(
          ownerPublicId: userPublicId,
          dryRun: dryRun,
          requestedPublicIds: context.RequestedPublicIds,
          assignedNewPublicIds: context.ToAssignNew.Select(role => role.PublicId).ToList(),
          assignedReactivatedPublicIds: context.ToReactivate.Select(relation => relation.Role.PublicId).ToList(),
          unassignedPublicIds: context.ToUnassign.Select(relation => relation.Role.PublicId).ToList(),
          unchangedPublicIds: context.Unchanged.Select(relation => relation.Role.PublicId).ToList()));
    }

    public async Task<OperationResult<RoleUserAssignmentBulkResult>> UpdateRoleUsersAsync(Guid rolePublicId, IReadOnlyCollection<Guid> userPublicIds, bool dryRun, CancellationToken cancellationToken)
    {
        var role = await _repository.GetActiveRoleByPublicIdAsync(rolePublicId, cancellationToken);
        if (role == null)
        {
            return OperationResult.Fail<RoleUserAssignmentBulkResult>(
                ErrorInfo.Status404NotFound,
                string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.ActiveRoleNotFound.Message, rolePublicId));
        }

        var contextResult = await BuildRoleUserContextAsync(role.Id, userPublicIds, cancellationToken);
        if (!contextResult.Success)
        {
            return OperationResult.Fail<RoleUserAssignmentBulkResult>(contextResult.ErrorInfo!, contextResult.Error!);
        }

        var context = contextResult.Data!;
        if (!dryRun)
        {
            var execResult = await ExecuteRoleUserChangesAsync(rolePublicId, context, cancellationToken);
            if (!execResult.Success)
            {
                return OperationResult.Fail<RoleUserAssignmentBulkResult>(execResult.ErrorInfo!, execResult.Error!);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Ok(BuildBulkResult(
          ownerPublicId: rolePublicId,
          dryRun: dryRun,
          requestedPublicIds: context.RequestedPublicIds,
          assignedNewPublicIds: context.ToAssignNew.Select(user => user.PublicId).ToList(),
          assignedReactivatedPublicIds: context.ToReactivate.Select(relation => relation.User.PublicId).ToList(),
          unassignedPublicIds: context.ToUnassign.Select(relation => relation.User.PublicId).ToList(),
          unchangedPublicIds: context.Unchanged.Select(relation => relation.User.PublicId).ToList()));
    }

    private async Task<OperationResult<UserRoleContext>> BuildUserRoleContextAsync(long userId, IReadOnlyCollection<Guid> rolePublicIds, CancellationToken cancellationToken)
    {
        var requestedPublicIds = rolePublicIds.Where(publicId => publicId != Guid.Empty).Distinct().ToList();
        var requestedRoles = await _repository.GetActiveRolesByPublicIdsAsync(requestedPublicIds, cancellationToken);
        if (requestedRoles.Count != requestedPublicIds.Count)
        {
            return MissingEntitiesResult<UserRoleContext>(
              requestedPublicIds,
              requestedRoles.Select(role => role.PublicId),
                  UserRoleAssignmentValidationTypes.ActiveRolesNotFound.Message);
        }

        var currentVisibleRelations = await _repository.GetVisibleRelationsByUserIdAsync(userId, cancellationToken);
        var currentVisibleRoleIds = currentVisibleRelations.Select(relation => relation.RoleId).ToHashSet();
        var requestedRoleIds = requestedRoles.Select(role => role.Id).ToHashSet();

        var deletedRelations = await _repository.GetDeletedRelationsByUserIdAndRoleIdsAsync(userId, requestedRoleIds.ToList(), cancellationToken);
        var deletedRoleIds = deletedRelations.Select(relation => relation.RoleId).ToHashSet();

        var context = new UserRoleContext(
          RequestedPublicIds: requestedPublicIds,
          RequestedRoles: requestedRoles,
          ToUnassign: currentVisibleRelations.Where(relation => !requestedRoleIds.Contains(relation.RoleId)).ToList().AsReadOnly(),
          ToReactivate: deletedRelations.Where(relation => !currentVisibleRoleIds.Contains(relation.RoleId)).ToList().AsReadOnly(),
          ToAssignNew: requestedRoles.Where(role => !currentVisibleRoleIds.Contains(role.Id) && !deletedRoleIds.Contains(role.Id)).ToList().AsReadOnly(),
          Unchanged: currentVisibleRelations.Where(relation => requestedRoleIds.Contains(relation.RoleId)).ToList().AsReadOnly());

        return OperationResult.Ok(context);
    }

    private async Task<OperationResult<RoleUserContext>> BuildRoleUserContextAsync(long roleId, IReadOnlyCollection<Guid> userPublicIds, CancellationToken cancellationToken)
    {
        var requestedPublicIds = userPublicIds.Where(publicId => publicId != Guid.Empty).Distinct().ToList();
        var requestedUsers = await _repository.GetActiveUsersByPublicIdsAsync(requestedPublicIds, cancellationToken);
        if (requestedUsers.Count != requestedPublicIds.Count)
        {
            return MissingEntitiesResult<RoleUserContext>(
              requestedPublicIds,
              requestedUsers.Select(user => user.PublicId),
                  UserRoleAssignmentValidationTypes.ActiveUsersNotFound.Message);
        }

        var currentVisibleRelations = await _repository.GetVisibleRelationsByRoleIdAsync(roleId, cancellationToken);
        var currentVisibleUserIds = currentVisibleRelations.Select(relation => relation.UserId).ToHashSet();
        var requestedUserIds = requestedUsers.Select(user => user.Id).ToHashSet();

        var deletedRelations = await _repository.GetDeletedRelationsByRoleIdAndUserIdsAsync(roleId, requestedUserIds.ToList(), cancellationToken);
        var deletedUserIds = deletedRelations.Select(relation => relation.UserId).ToHashSet();

        var context = new RoleUserContext(
          RequestedPublicIds: requestedPublicIds,
          RequestedUsers: requestedUsers,
          ToUnassign: currentVisibleRelations.Where(relation => !requestedUserIds.Contains(relation.UserId)).ToList().AsReadOnly(),
          ToReactivate: deletedRelations.Where(relation => !currentVisibleUserIds.Contains(relation.UserId)).ToList().AsReadOnly(),
          ToAssignNew: requestedUsers.Where(user => !currentVisibleUserIds.Contains(user.Id) && !deletedUserIds.Contains(user.Id)).ToList().AsReadOnly(),
          Unchanged: currentVisibleRelations.Where(relation => requestedUserIds.Contains(relation.UserId)).ToList().AsReadOnly());

        return OperationResult.Ok(context);
    }

    private async Task<OperationResult<bool>> ExecuteUserRoleChangesAsync(Guid userPublicId, UserRoleContext context, CancellationToken cancellationToken)
    {
        foreach (var relation in context.ToUnassign)
        {
            var result = await _deleteUserRolesByPublicIdService.ExecuteAsync(userPublicId, relation.Role.PublicId, cancellationToken, false);
            if (!result.Success)
            {
                return OperationResult.Fail<bool>(result.ErrorInfo!, result.Error!);
            }
        }

        foreach (var relation in context.ToReactivate)
        {
            var result = await _restoreUserRolesByPublicIdService.ExecuteAsync(userPublicId, relation.Role.PublicId, cancellationToken, false);
            if (!result.Success)
            {
                return OperationResult.Fail<bool>(result.ErrorInfo!, result.Error!);
            }
        }

        foreach (var role in context.ToAssignNew)
        {
            var result = await _createUserRolesService.ExecuteAsync(userPublicId, role.PublicId, cancellationToken, false);
            if (!result.Success)
            {
                return OperationResult.Fail<bool>(result.ErrorInfo!, result.Error!);
            }
        }

        return OperationResult.Ok(true);
    }

    private async Task<OperationResult<bool>> ExecuteRoleUserChangesAsync(Guid rolePublicId, RoleUserContext context, CancellationToken cancellationToken)
    {
        foreach (var relation in context.ToUnassign)
        {
            var result = await _deleteUserRolesByPublicIdService.ExecuteAsync(relation.User.PublicId, rolePublicId, cancellationToken, false);
            if (!result.Success)
            {
                return OperationResult.Fail<bool>(result.ErrorInfo!, result.Error!);
            }
        }

        foreach (var relation in context.ToReactivate)
        {
            var result = await _restoreUserRolesByPublicIdService.ExecuteAsync(relation.User.PublicId, rolePublicId, cancellationToken, false);
            if (!result.Success)
            {
                return OperationResult.Fail<bool>(result.ErrorInfo!, result.Error!);
            }
        }

        foreach (var user in context.ToAssignNew)
        {
            var result = await _createUserRolesService.ExecuteAsync(user.PublicId, rolePublicId, cancellationToken, false);
            if (!result.Success)
            {
                return OperationResult.Fail<bool>(result.ErrorInfo!, result.Error!);
            }
        }

        return OperationResult.Ok(true);
    }

    private static OperationResult<T> MissingEntitiesResult<T>(
        IReadOnlyCollection<Guid> requestedPublicIds,
        IEnumerable<Guid> foundPublicIds,
        string messageTemplate)
    {
        var found = foundPublicIds.ToHashSet();
        var missing = requestedPublicIds.Where(publicId => !found.Contains(publicId));
        return OperationResult.Fail<T>(ErrorInfo.Status404NotFound, string.Format(CultureInfo.InvariantCulture, messageTemplate, string.Join(", ", missing)));
    }

    private static RoleUserAssignmentBulkResult BuildBulkResult(
        Guid ownerPublicId,
        bool dryRun,
        IReadOnlyList<Guid> requestedPublicIds,
        List<Guid> assignedNewPublicIds,
        List<Guid> assignedReactivatedPublicIds,
        List<Guid> unassignedPublicIds,
        List<Guid> unchangedPublicIds)
    {
        return new RoleUserAssignmentBulkResult(
          OwnerPublicId: ownerPublicId,
          DryRun: dryRun,
          TotalRequested: requestedPublicIds.Count,
          TotalAssignedNew: assignedNewPublicIds.Count,
          TotalAssignedReactivated: assignedReactivatedPublicIds.Count,
          TotalUnassigned: unassignedPublicIds.Count,
          TotalUnchanged: unchangedPublicIds.Count,
          ExecutedAt: DateTime.UtcNow,
          RequestedPublicIds: requestedPublicIds,
          AssignedNewPublicIds: assignedNewPublicIds,
          AssignedReactivatedPublicIds: assignedReactivatedPublicIds,
          UnassignedPublicIds: unassignedPublicIds,
          UnchangedPublicIds: unchangedPublicIds);
    }
}
