using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public class CreateRolePermissionsCommandValidator : AbstractValidator<CreateRolePermissionsCommand>
{
    public CreateRolePermissionsCommandValidator()
    {
        RuleFor(command => command.RolePublicId)
          .ValidateRolePublicIdForPermissionAssignment();

        RuleFor(command => command.PermissionPublicId)
          .ValidatePermissionPublicIdForRoleAssignment();
    }
}