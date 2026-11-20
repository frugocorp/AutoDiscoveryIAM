// -----------------------------------------------------------------------
// <copyright file="ExecuteEmailChangeRequestDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;

public static class ExecuteEmailChangeRequestDI
{
  public static IServiceCollection AddExecuteEmailChangeRequestDI(this IServiceCollection services)
  {
    services.AddScoped<IExecuteEmailChangeRequestRepository, ExecuteEmailChangeRequestRepository>();
    services.AddScoped<ExecuteEmailChangeRequestHandler>();
    return services;
  }
}
