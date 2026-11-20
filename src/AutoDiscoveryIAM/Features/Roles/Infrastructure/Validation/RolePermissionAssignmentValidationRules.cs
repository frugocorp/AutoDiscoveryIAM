using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

public static class RolePermissionAssignmentValidationRules
{
  public static IRuleBuilderOptions<T, Guid> ValidateRolePublicIdForPermissionAssignment<T>(this IRuleBuilder<T, Guid> ruleBuilder)
  {
    return ruleBuilder
      .NotEmpty()
      .WithMessage(RolePermissionAssignmentValidationTypes.RolePublicIdRequired.Message);
  }

  public static IRuleBuilderOptions<T, Guid> ValidatePermissionPublicIdForRoleAssignment<T>(this IRuleBuilder<T, Guid> ruleBuilder)
  {
    return ruleBuilder
      .NotEmpty()
      .WithMessage(RolePermissionAssignmentValidationTypes.PermissionPublicIdRequired.Message);
  }

  public static IRuleBuilderOptions<T, IReadOnlyCollection<Guid>> ValidatePermissionPublicIdsForBulkAssignment<T>(this IRuleBuilder<T, IReadOnlyCollection<Guid>> ruleBuilder)
  {
    return ruleBuilder
      .NotNull()
      .WithMessage(RolePermissionAssignmentValidationTypes.PermissionPublicIdsRequired.Message)
      .Must(ids => ids != null && ids.All(id => id != Guid.Empty))
      .WithMessage(RolePermissionAssignmentValidationTypes.PermissionPublicIdsContainEmpty.Message)
      .Must(ids => ids == null || ids.Count <= PaginationDefaults.MaxBulkAssignmentSize)
      .WithMessage(RolePermissionAssignmentValidationTypes.PermissionPublicIdsExceedMaxBulkSize.Message);
  }

  public static IRuleBuilderOptions<T, IReadOnlyCollection<Guid>> ValidateUserPublicIdsForBulkRoleAssignment<T>(this IRuleBuilder<T, IReadOnlyCollection<Guid>> ruleBuilder)
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
