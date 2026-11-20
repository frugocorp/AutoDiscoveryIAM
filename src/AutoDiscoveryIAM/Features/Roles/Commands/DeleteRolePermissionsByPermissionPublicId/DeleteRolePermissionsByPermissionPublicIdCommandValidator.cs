using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public class DeleteRolePermissionsByPermissionPublicIdCommandValidator : AbstractValidator<DeleteRolePermissionsByPermissionPublicIdCommand>
{
    public DeleteRolePermissionsByPermissionPublicIdCommandValidator()
    {
        RuleFor(command => command.RolePublicId)
          .ValidateRolePublicIdForPermissionAssignment();

        RuleFor(command => command.PermissionPublicId)
          .ValidatePermissionPublicIdForRoleAssignment();
    }
}