using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;

public record GetUsersByPublicIdQuery(
  Guid RolePublicId,
  int PageNumber = PaginationDefaults.DefaultPageNumber,
  int PageSize = PaginationDefaults.DefaultPageSize);
