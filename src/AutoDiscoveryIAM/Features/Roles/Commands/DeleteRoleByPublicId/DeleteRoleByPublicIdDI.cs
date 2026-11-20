// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

/// <summary>
/// Configuración de inyección de dependencias para DeleteRoleByPublicId.
/// </summary>
public static class DeleteRoleByPublicIdDI
{
    /// <summary>
    /// Registra los servicios de DeleteRoleByPublicId en el contenedor de DI.
    /// </summary>
    /// <param name="services">Colección de servicios donde registrar las dependencias.</param>
    /// <returns>La misma colección de servicios para permitir fluent chaining.</returns>
    public static IServiceCollection AddDeleteRoleByPublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IDeleteRoleByPublicIdRepository, DeleteRoleByPublicIdRepository>();
        services.AddScoped<DeleteRoleByPublicIdHandler>();
        services.AddScoped<DeleteRoleByPublicIdCommandValidator>();
        // Logger de dominio (IRoleLogger se registra globalmente en RolesDI)
        return services;
    }
}