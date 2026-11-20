using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public static class ClearBulkRoleHiddenRelationsDI
{
  public static IServiceCollection AddClearBulkRoleHiddenRelationsDI(this IServiceCollection services)
  {
    services.AddScoped<IClearBulkRoleHiddenRelationsRepository, ClearBulkRoleHiddenRelationsRepository>();
    services.AddScoped<IClearBulkRoleHiddenRelationsService, ClearBulkRoleHiddenRelationsService>();
    services.AddScoped<ClearBulkRoleHiddenRelationsHandler>();
    services.AddScoped<ClearBulkRoleHiddenRelationsCommandValidator>();
    return services;
  }
}
