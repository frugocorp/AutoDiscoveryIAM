using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;

public record UserRoleContext(
    IReadOnlyList<Guid> RequestedPublicIds,
    IReadOnlyList<Role> RequestedRoles,
    IReadOnlyList<UserRole> ToUnassign,
    IReadOnlyList<UserRole> ToReactivate,
    IReadOnlyList<Role> ToAssignNew,
    IReadOnlyList<UserRole> Unchanged);
