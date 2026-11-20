using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

public static class UserRoleAssignmentValidationRules
{
    public static IRuleBuilderOptions<T, Guid> ValidateUserPublicIdForRoleAssignment<T>(this IRuleBuilder<T, Guid> ruleBuilder)
    {
        return ruleBuilder
          .NotEmpty()
          .WithMessage(UserRoleAssignmentValidationTypes.UserPublicIdRequired.Message);
    }

    public static IRuleBuilderOptions<T, Guid> ValidateRolePublicIdForUserAssignment<T>(this IRuleBuilder<T, Guid> ruleBuilder)
    {
        return ruleBuilder
          .NotEmpty()
          .WithMessage(UserRoleAssignmentValidationTypes.RolePublicIdRequired.Message);
    }

    public static IRuleBuilderOptions<T, IReadOnlyCollection<Guid>> ValidateRolePublicIdsForBulkAssignment<T>(this IRuleBuilder<T, IReadOnlyCollection<Guid>> ruleBuilder)
    {
        return ruleBuilder
          .NotNull()
          .WithMessage(UserRoleAssignmentValidationTypes.RolePublicIdsRequired.Message)
          .Must(ids => ids != null && ids.All(id => id != Guid.Empty))
          .WithMessage(UserRoleAssignmentValidationTypes.RolePublicIdsContainEmpty.Message)
          .Must(ids => ids == null || ids.Count <= PaginationDefaults.MaxBulkAssignmentSize)
          .WithMessage(UserRoleAssignmentValidationTypes.RolePublicIdsExceedMaxBulkSize.Message);
    }

    public static IRuleBuilderOptions<T, IReadOnlyCollection<Guid>> ValidateUserPublicIdsForBulkAssignment<T>(this IRuleBuilder<T, IReadOnlyCollection<Guid>> ruleBuilder)
    {
        return ruleBuilder
          .NotNull()
          .WithMessage(UserRoleAssignmentValidationTypes.UserPublicIdsRequired.Message)
          .Must(ids => ids != null && ids.All(id => id != Guid.Empty))
          .WithMessage(UserRoleAssignmentValidationTypes.UserPublicIdsContainEmpty.Message)
          .Must(ids => ids == null || ids.Count <= PaginationDefaults.MaxBulkAssignmentSize)
          .WithMessage(UserRoleAssignmentValidationTypes.UserPublicIdsExceedMaxBulkSize.Message);
    }
}
