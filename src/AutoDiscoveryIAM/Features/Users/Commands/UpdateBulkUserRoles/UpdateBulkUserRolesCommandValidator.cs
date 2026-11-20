using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public class UpdateBulkUserRolesCommandValidator : AbstractValidator<UpdateBulkUserRolesCommand>
{
    public UpdateBulkUserRolesCommandValidator()
    {
        RuleFor(command => command.UserPublicId)
          .ValidateUserPublicIdForRoleAssignment();

        RuleFor(command => command.RolePublicIds)
          .ValidateRolePublicIdsForBulkAssignment();
    }
}