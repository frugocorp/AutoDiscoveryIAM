// -----------------------------------------------------------------------
// <copyright file="CreateRoleDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;

public static class CreateRoleDI
{
    public static IServiceCollection AddCreateRoleDI(this IServiceCollection services)
    {
        services.AddScoped<ICreateRoleRepository, CreateRoleRepository>();
        services.AddScoped<CreateRoleHandler>();
        services.AddScoped<CreateRoleCommandValidator>();
        return services;
    }
}
