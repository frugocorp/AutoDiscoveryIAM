// -----------------------------------------------------------------------
// <copyright file="GetPermissionsDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;

public static class GetPermissionsDI
{
  public static IServiceCollection AddGetPermissionsDI(this IServiceCollection services)
  {
    services.AddScoped<IGetPermissionsRepository, GetPermissionsRepository>();
    services.AddScoped<GetPermissionsHandler>();
    return services;
  }
}
