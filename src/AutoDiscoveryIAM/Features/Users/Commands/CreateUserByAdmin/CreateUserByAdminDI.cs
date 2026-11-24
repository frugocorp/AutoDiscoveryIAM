// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Registro de dependencias del slice <c>CreateUserByAdmin</c>.
/// </summary>
public static class CreateUserByAdminDI
{
  /// <summary>
  /// Registra el repositorio, el handler, el validador del slice y <see cref="IUserProvisioningService"/>,
  /// el alta desde el código del consumidor.
  /// </summary>
  /// <param name="services">Contenedor de servicios.</param>
  /// <returns>El mismo contenedor.</returns>
  public static IServiceCollection AddCreateUserByAdminDI(this IServiceCollection services)
  {
    services.AddScoped<ICreateUserByAdminRepository, CreateUserByAdminRepository>();
    services.AddScoped<CreateUserByAdminHandler>();
    services.AddScoped<CreateUserByAdminCommandValidator>();
    services.AddScoped<IUserProvisioningService, UserProvisioningService>();
    return services;
  }
}
