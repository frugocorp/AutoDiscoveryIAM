// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// Configuración de inyección de dependencias para UpdateRoleByPublicId.
/// </summary>
public static class UpdateRoleByPublicIdDI
{
  /// <summary>
  /// Configura las dependencias del slice UpdateRoleByPublicId.
  /// </summary>
  /// <param name="services">Colección de servicios para inyección de dependencias.</param>
  /// <returns>La colección de servicios para encadenar.</returns>
  public static IServiceCollection AddUpdateRoleByPublicIdDI(this IServiceCollection services)
  {
    services.AddScoped<IUpdateRoleByPublicIdRepository, UpdateRoleByPublicIdRepository>();
    services.AddScoped<UpdateRoleByPublicIdHandler>();
    services.AddScoped<UpdateRoleByPublicIdCommandValidator>();

    return services;
  }
}
