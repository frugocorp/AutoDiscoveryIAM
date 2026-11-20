// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdQueryValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

public class GetUserByPublicIdQueryValidator : AbstractValidator<GetUserByPublicIdQuery>
{
  public GetUserByPublicIdQueryValidator()
  {
    RuleFor(x => x.PublicId)
        .NotEmpty()
        .WithMessage("PublicId es requerido");
  }
}
