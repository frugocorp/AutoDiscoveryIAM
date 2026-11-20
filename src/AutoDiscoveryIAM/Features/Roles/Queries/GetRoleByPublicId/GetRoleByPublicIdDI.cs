// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// Registro de dependencias para GetRoleByPublicId.
/// </summary>
public static class GetRoleByPublicIdDI
{
    public static IServiceCollection AddGetRoleByPublicIdDI(this IServiceCollection services)
    {
        services.AddScoped<IGetRoleByPublicIdRepository, GetRoleByPublicIdRepository>();
        services.AddScoped<GetRoleByPublicIdHandler>();
        services.AddScoped<GetRoleByPublicIdQueryValidator>();
        return services;
    }
}
