// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

public static class RequestEmailChangeDI
{
  public static IServiceCollection AddRequestEmailChangeDI(this IServiceCollection services)
  {
    services.AddScoped<IRequestEmailChangeRepository, RequestEmailChangeRepository>();
    services.AddScoped<RequestEmailChangeHandler>();
    services.AddScoped<RequestEmailChangeCommandValidator>();
    return services;
  }
}
