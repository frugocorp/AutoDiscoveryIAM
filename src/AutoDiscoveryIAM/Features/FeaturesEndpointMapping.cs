
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features;

public static class FeaturesEndpointMapping
{
  public static void MapAllFeatureEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapUsersEndpoints();
    app.MapAuthEndpoints();
    app.MapPermissionsEndpoints();
    app.MapRolesEndpoints();
  }
}
