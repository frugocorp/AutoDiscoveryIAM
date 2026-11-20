// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

public class ReviewEmailChangeRequestCommandValidator : AbstractValidator<ReviewEmailChangeRequestCommand>
{
  public ReviewEmailChangeRequestCommandValidator()
  {
    RuleFor(x => x.RejectionReason)
      .NotEmpty()
      .When(x => !x.Approve)
      .WithMessage("El motivo del rechazo es obligatorio cuando no se aprueba la solicitud.");
  }
}
