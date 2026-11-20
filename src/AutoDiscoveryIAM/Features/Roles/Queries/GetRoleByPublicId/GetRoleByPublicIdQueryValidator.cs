// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdQueryValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// Validador para GetRoleByPublicIdQuery.
/// </summary>
public class GetRoleByPublicIdQueryValidator : AbstractValidator<GetRoleByPublicIdQuery>
{
    public GetRoleByPublicIdQueryValidator()
    {
        RuleFor(x => x.PublicId)
            .ValidateRolePublicId();
    }
}
