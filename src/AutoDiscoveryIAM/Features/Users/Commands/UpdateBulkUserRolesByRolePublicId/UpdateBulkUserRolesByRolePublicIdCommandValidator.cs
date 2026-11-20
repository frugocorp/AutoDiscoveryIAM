using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public class UpdateBulkUserRolesByRolePublicIdCommandValidator : AbstractValidator<UpdateBulkUserRolesByRolePublicIdCommand>
{
    public UpdateBulkUserRolesByRolePublicIdCommandValidator()
    {
        RuleFor(command => command.RolePublicId)
          .ValidateRolePublicIdForUserAssignment();

        RuleFor(command => command.UserPublicIds)
          .ValidateUserPublicIdsForBulkAssignment();
    }
}