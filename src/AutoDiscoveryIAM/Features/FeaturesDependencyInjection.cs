using Microsoft.Extensions.DependencyInjection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features;

public static class FeaturesDI
{
  public static IServiceCollection AddFeaturesDI(this IServiceCollection services)
  {
    services.AddUsersDI();
    services.AddAuthDI();
    services.AddPermissionsDI();
    services.AddRolesDI();
    return services;
  }
}
