// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

public static class GetUserByPublicIdDI
{
  public static IServiceCollection AddGetUserByPublicIdDI(this IServiceCollection services)
  {
    services.AddScoped<IGetUserByPublicIdRepository, GetUserByPublicIdRepository>();
    services.AddScoped<GetUserByPublicIdHandler>();
    services.AddScoped<GetUserByPublicIdQueryValidator>();
    return services;
  }
}
