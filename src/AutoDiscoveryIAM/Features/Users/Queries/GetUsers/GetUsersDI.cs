// -----------------------------------------------------------------------
// <copyright file="GetUsersDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

public static class GetUsersDI
{
  public static IServiceCollection AddGetUsersDI(this IServiceCollection services)
  {
    services.AddScoped<IGetUsersRepository, GetUsersRepository>();
    services.AddScoped<GetUsersHandler>();
    return services;
  }
}
