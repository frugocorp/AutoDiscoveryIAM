using FluentValidation;
using TuProyecto.Features.UserProfiles.Infrastructure.Validation;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

/// <summary>
/// Requiere el paquete FluentValidation -- ver "Extras opcionales: Validator, OpenAPI y Logging"
/// en el README de AutoDiscoveryIAM. Se ejecuta en el EndPoint, antes de llamar al Handler.
/// </summary>
public class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand>
{
  public CreateUserProfileCommandValidator()
  {
    RuleFor(x => x.UserPublicId).ValidateUserPublicId();
    RuleFor(x => x.Occupation).ValidateOccupation();
    RuleFor(x => x.Biography).ValidateBiography();
    RuleFor(x => x.MaritalStatus).ValidateMaritalStatus();
  }
}
