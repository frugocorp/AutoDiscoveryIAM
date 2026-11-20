using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public class CreateUserRolesCommandValidator : AbstractValidator<CreateUserRolesCommand>
{
    public CreateUserRolesCommandValidator()
    {
        RuleFor(command => command.UserPublicId)
          .ValidateUserPublicIdForRoleAssignment();

        RuleFor(command => command.RolePublicId)
          .ValidateRolePublicIdForUserAssignment();
    }
}
