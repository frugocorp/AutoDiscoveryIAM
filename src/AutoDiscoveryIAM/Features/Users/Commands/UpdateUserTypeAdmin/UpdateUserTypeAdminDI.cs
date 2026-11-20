// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

public static class UpdateUserTypeAdminDI
{
  public static IServiceCollection AddUpdateUserTypeAdminDI(this IServiceCollection services)
  {
    services.AddScoped<IUpdateUserTypeAdminRepository, UpdateUserTypeAdminRepository>();
    services.AddScoped<UpdateUserTypeAdminHandler>();
    services.AddScoped<UpdateUserTypeAdminCommandValidator>();
    return services;
  }
}
