// -----------------------------------------------------------------------
// <copyright file="GetRolesDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;

public static class GetRolesDI
{
  public static IServiceCollection AddGetRolesDI(this IServiceCollection services)
  {
    services.AddScoped<IGetRolesRepository, GetRolesRepository>();
    services.AddScoped<GetRolesHandler>();
    return services;
  }
}
