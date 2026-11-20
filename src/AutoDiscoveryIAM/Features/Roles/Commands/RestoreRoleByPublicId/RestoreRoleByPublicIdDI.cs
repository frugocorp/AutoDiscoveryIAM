// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

public static class RestoreRoleByPublicIdDI
{
  public static IServiceCollection AddRestoreRoleByPublicId(this IServiceCollection services)
  {
    // Handler
    services.AddScoped<RestoreRoleByPublicIdHandler>();
    // Validator
    services.AddScoped<RestoreRoleByPublicIdCommandValidator>();
    // Repositorio
    services.AddScoped<IRestoreRoleByPublicIdRepository, RestoreRoleByPublicIdRepository>();
    return services;
  }
}