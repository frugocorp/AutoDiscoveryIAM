using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public interface IUpdateBulkRolePermissionsService
{
    Task<OperationResult<UpdateBulkRolePermissionsResponseDto>> ExecuteAsync(Guid rolePublicId, IReadOnlyCollection<Guid> permissionPublicIds, bool dryRun, CancellationToken cancellationToken);
}

public class UpdateBulkRolePermissionsService(
    IUpdateBulkRolePermissionsRepository repository,
    ICreateRolePermissionsService createService,
    IDeleteRolePermissionsByPermissionPublicIdService deleteService,
    IRestoreRolePermissionsByPermissionPublicIdService restoreService) : IUpdateBulkRolePermissionsService
{
    private readonly IUpdateBulkRolePermissionsRepository _repository = repository;
    private readonly ICreateRolePermissionsService _createService = createService;
    private readonly IDeleteRolePermissionsByPermissionPublicIdService _deleteService = deleteService;
    private readonly IRestoreRolePermissionsByPermissionPublicIdService _restoreService = restoreService;

    public async Task<OperationResult<UpdateBulkRolePermissionsResponseDto>> ExecuteAsync(Guid rolePublicId, IReadOnlyCollection<Guid> permissionPublicIds, bool dryRun, CancellationToken cancellationToken)
    {
        var role = await _repository.GetActiveRoleByPublicIdAsync(rolePublicId, cancellationToken);
        if (role == null)
        {
            return OperationResult.Fail<UpdateBulkRolePermissionsResponseDto>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.ActiveRoleNotFound.Message, rolePublicId));
        }

        var diff = await BuildDiffAsync(role, permissionPublicIds, cancellationToken);
        if (!diff.Valid)
        {
            return OperationResult.Fail<UpdateBulkRolePermissionsResponseDto>(ErrorInfo.Status404NotFound, diff.ErrorMessage!);
        }

        if (!dryRun)
        {
            var error = await ApplyChangesAsync(rolePublicId, diff, cancellationToken);
            if (error != null)
            {
                return error;
            }
        }

        return OperationResult.Ok(BuildResult(rolePublicId, dryRun, diff));
    }

    private async Task<BulkPermissionDiff> BuildDiffAsync(
      FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities.Role role,
      IReadOnlyCollection<Guid> permissionPublicIds,
      CancellationToken cancellationToken)
    {
        var requestedPublicIds = permissionPublicIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var requestedPermissions = await _repository.GetActivePermissionsByPublicIdsAsync(requestedPublicIds, cancellationToken);

        if (requestedPermissions.Count != requestedPublicIds.Count)
        {
            var foundPublicIds = requestedPermissions.Select(p => p.PublicId).ToHashSet();
            var missingIds = requestedPublicIds.Where(id => !foundPublicIds.Contains(id));
            return BulkPermissionDiff.Failure(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                RolePermissionAssignmentValidationTypes.ActivePermissionsNotFound.Message,
                string.Join(", ", missingIds)));
        }

        var currentVisible = await _repository.GetVisibleRelationsByRoleIdAsync(role.Id, cancellationToken);
        var currentVisibleIds = currentVisible.Select(r => r.PermissionId).ToHashSet();
        var requestedIds = requestedPermissions.Select(p => p.Id).ToHashSet();
        var deletedRelations = await _repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, requestedIds, cancellationToken);
        var deletedIds = deletedRelations.Select(r => r.PermissionId).ToHashSet();

        return new BulkPermissionDiff(
          requestedPublicIds,
          requestedPermissions.ToList(),
          toUnassign: currentVisible.Where(r => !requestedIds.Contains(r.PermissionId)).ToList(),
          toReactivate: deletedRelations.Where(r => !currentVisibleIds.Contains(r.PermissionId)).ToList(),
          toAssignNew: requestedPermissions.Where(p => !currentVisibleIds.Contains(p.Id) && !deletedIds.Contains(p.Id)).ToList(),
          unchanged: currentVisible.Where(r => requestedIds.Contains(r.PermissionId)).ToList());
    }

    private async Task<OperationResult<UpdateBulkRolePermissionsResponseDto>?> ApplyChangesAsync(Guid rolePublicId, BulkPermissionDiff diff, CancellationToken cancellationToken)
    {
        foreach (var relation in diff.ToUnassign)
        {
            var result = await _deleteService.ExecuteAsync(rolePublicId, relation.Permission.PublicId, cancellationToken, true);
            if (!result.Success)
            {
                return OperationResult.Fail<UpdateBulkRolePermissionsResponseDto>(result.ErrorInfo!, result.Error!);
            }
        }

        foreach (var relation in diff.ToReactivate)
        {
            var result = await _restoreService.ExecuteAsync(rolePublicId, relation.Permission.PublicId, cancellationToken, true);
            if (!result.Success)
            {
                return OperationResult.Fail<UpdateBulkRolePermissionsResponseDto>(result.ErrorInfo!, result.Error!);
            }
        }

        foreach (var permission in diff.ToAssignNew)
        {
            var result = await _createService.ExecuteAsync(rolePublicId, permission.PublicId, cancellationToken, true);
            if (!result.Success)
            {
                return OperationResult.Fail<UpdateBulkRolePermissionsResponseDto>(result.ErrorInfo!, result.Error!);
            }
        }

        return null;
    }

    private static UpdateBulkRolePermissionsResponseDto BuildResult(Guid rolePublicId, bool dryRun, BulkPermissionDiff diff)
      => new(
        RolePublicId: rolePublicId,
        DryRun: dryRun,
        TotalRequested: diff.RequestedPublicIds.Count,
        TotalAssignedNew: diff.ToAssignNew.Count,
        TotalAssignedReactivated: diff.ToReactivate.Count,
        TotalUnassigned: diff.ToUnassign.Count,
        TotalUnchanged: diff.Unchanged.Count,
        ExecutedAt: DateTime.UtcNow,
        RequestedPublicIds: diff.RequestedPublicIds,
        AssignedNewPublicIds: diff.ToAssignNew.ConvertAll(p => p.PublicId),
        AssignedReactivatedPublicIds: diff.ToReactivate.ConvertAll(r => r.Permission.PublicId),
        UnassignedPublicIds: diff.ToUnassign.ConvertAll(r => r.Permission.PublicId),
        UnchangedPublicIds: diff.Unchanged.ConvertAll(r => r.Permission.PublicId));

    private sealed class BulkPermissionDiff
    {
        public bool Valid { get; }
        public string? ErrorMessage { get; }
        public List<Guid> RequestedPublicIds { get; }
        public List<Permission> RequestedPermissions { get; }
        public List<RolePermission> ToUnassign { get; }
        public List<RolePermission> ToReactivate { get; }
        public List<Permission> ToAssignNew { get; }
        public List<RolePermission> Unchanged { get; }

        public BulkPermissionDiff(
          List<Guid> requestedPublicIds,
          List<Permission> requestedPermissions,
          List<RolePermission> toUnassign,
          List<RolePermission> toReactivate,
          List<Permission> toAssignNew,
          List<RolePermission> unchanged)
        {
            Valid = true;
            RequestedPublicIds = requestedPublicIds;
            RequestedPermissions = requestedPermissions;
            ToUnassign = toUnassign;
            ToReactivate = toReactivate;
            ToAssignNew = toAssignNew;
            Unchanged = unchanged;
        }

        private BulkPermissionDiff(string errorMessage)
        {
            Valid = false;
            ErrorMessage = errorMessage;
            RequestedPublicIds = [];
            RequestedPermissions = [];
            ToUnassign = [];
            ToReactivate = [];
            ToAssignNew = [];
            Unchanged = [];
        }

        public static BulkPermissionDiff Failure(string errorMessage) => new(errorMessage);
    }
}
