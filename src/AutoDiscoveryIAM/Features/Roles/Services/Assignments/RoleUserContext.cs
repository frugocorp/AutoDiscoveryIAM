using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;

public record RoleUserContext(
    IReadOnlyList<Guid> RequestedPublicIds,
    IReadOnlyList<User> RequestedUsers,
    IReadOnlyList<UserRole> ToUnassign,
    IReadOnlyList<UserRole> ToReactivate,
    IReadOnlyList<User> ToAssignNew,
    IReadOnlyList<UserRole> Unchanged);
