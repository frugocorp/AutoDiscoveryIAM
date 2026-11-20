using FluentValidation;
using TuProyecto.Features.UserProfiles.Models.Catalogs;

namespace TuProyecto.Features.UserProfiles.Infrastructure.Validation;

/// <summary>
/// Reglas de FluentValidation reutilizables para UserProfile, como extension methods sobre
/// IRuleBuilder -- así CreateUserProfileCommandValidator (y cualquier otro validador futuro que
/// toque campos de UserProfile, ej. un futuro UpdateUserProfileCommandValidator) las reutiliza en
/// vez de reescribirlas. Mismo patrón que usa la librería en Features/Roles/Infrastructure/Validation/RoleValidationRules.cs.
/// </summary>
public static class UserProfileValidationRules
{
  public static IRuleBuilderOptions<T, Guid> ValidateUserPublicId<T>(this IRuleBuilder<T, Guid> ruleBuilder)
  {
    return ruleBuilder.NotEmpty().WithMessage(UserProfileValidationTypes.UserPublicIdMessages.Required);
  }

  public static IRuleBuilderOptions<T, string> ValidateOccupation<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
      .NotEmpty().WithMessage(UserProfileValidationTypes.OccupationMessages.Required)
      .MaximumLength(200).WithMessage(UserProfileValidationTypes.OccupationMessages.MaxLength);
  }

  public static IRuleBuilderOptions<T, string> ValidateBiography<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder.NotEmpty().WithMessage(UserProfileValidationTypes.BiographyMessages.Required);
  }

  public static IRuleBuilderOptions<T, string> ValidateMaritalStatus<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
      .Must(status => MaritalStatusCatalog.GetAll().Contains(status))
      .WithMessage(UserProfileValidationTypes.MaritalStatusMessages.Invalid);
  }
}
