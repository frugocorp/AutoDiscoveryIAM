// -----------------------------------------------------------------------
// <copyright file="CreateUserDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

public static class CreateUserDI
{
  public static IServiceCollection AddCreateUserDI(this IServiceCollection services)
  {
    services.AddScoped<ICreateUserRepository, CreateUserRepository>();
    services.AddScoped<CreateUserHandler>();
    services.AddScoped<CreateUserCommandValidator>();
    return services;
  }
}
