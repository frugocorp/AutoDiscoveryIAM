// -----------------------------------------------------------------------
// <copyright file="UpdateUserDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

public static class UpdateUserDI
{
  public static IServiceCollection AddUpdateUserDI(this IServiceCollection services)
  {
    services.AddScoped<IUpdateUserRepository, UpdateUserRepository>();
    services.AddScoped<UpdateUserHandler>();
    services.AddScoped<UpdateUserCommandValidator>();
    return services;
  }
}
