using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public class UpdateBulkRolePermissionsCommandValidator : AbstractValidator<UpdateBulkRolePermissionsCommand>
{
    public UpdateBulkRolePermissionsCommandValidator()
    {
        RuleFor(command => command.RolePublicId)
          .ValidateRolePublicIdForPermissionAssignment();

        RuleFor(command => command.PermissionPublicIds)
          .ValidatePermissionPublicIdsForBulkAssignment();
    }
}