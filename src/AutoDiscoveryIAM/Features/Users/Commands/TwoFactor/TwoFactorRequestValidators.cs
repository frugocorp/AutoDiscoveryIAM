using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;

public class EnableEmailTwoFactorRequestValidator : AbstractValidator<EnableEmailTwoFactorRequestDto>
{
  public EnableEmailTwoFactorRequestValidator()
  {
    RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("La contrasena actual es requerida.");
  }
}

public class EnrollAuthenticatorRequestValidator : AbstractValidator<EnrollAuthenticatorRequestDto>
{
  public EnrollAuthenticatorRequestValidator()
  {
    RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("La contrasena actual es requerida.");
  }
}

public class ConfirmAuthenticatorRequestValidator : AbstractValidator<ConfirmAuthenticatorRequestDto>
{
  public ConfirmAuthenticatorRequestValidator()
  {
    RuleFor(x => x.Code)
      .NotEmpty().WithMessage("El codigo de verificacion es requerido.")
      .Length(6).WithMessage("El codigo de verificacion debe tener 6 digitos.");
  }
}

public class DisableTwoFactorRequestValidator : AbstractValidator<DisableTwoFactorRequestDto>
{
  public DisableTwoFactorRequestValidator()
  {
    RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("La contrasena actual es requerida.");
  }
}
