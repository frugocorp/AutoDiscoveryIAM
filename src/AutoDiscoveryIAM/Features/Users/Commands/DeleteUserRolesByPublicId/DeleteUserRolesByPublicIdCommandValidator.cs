using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public class DeleteUserRolesByPublicIdCommandValidator : AbstractValidator<DeleteUserRolesByPublicIdCommand>
{
    public DeleteUserRolesByPublicIdCommandValidator()
    {
        RuleFor(command => command.UserPublicId)
          .ValidateUserPublicIdForRoleAssignment();

        RuleFor(command => command.RolePublicId)
          .ValidateRolePublicIdForUserAssignment();
    }
}