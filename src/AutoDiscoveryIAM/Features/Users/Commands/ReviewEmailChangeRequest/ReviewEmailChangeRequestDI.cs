// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

public static class ReviewEmailChangeRequestDI
{
  public static IServiceCollection AddReviewEmailChangeRequestDI(this IServiceCollection services)
  {
    services.AddScoped<IReviewEmailChangeRequestRepository, ReviewEmailChangeRequestRepository>();
    services.AddScoped<ReviewEmailChangeRequestHandler>();
    services.AddScoped<ReviewEmailChangeRequestCommandValidator>();
    return services;
  }
}
