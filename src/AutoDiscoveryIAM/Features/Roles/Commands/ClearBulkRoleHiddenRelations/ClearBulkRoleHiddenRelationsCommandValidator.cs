using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public class ClearBulkRoleHiddenRelationsCommandValidator : AbstractValidator<ClearBulkRoleHiddenRelationsCommand>
{
    public ClearBulkRoleHiddenRelationsCommandValidator()
    {
        RuleFor(command => command.RolePublicId)
          .ValidateRolePublicIdForPermissionAssignment();
    }
}