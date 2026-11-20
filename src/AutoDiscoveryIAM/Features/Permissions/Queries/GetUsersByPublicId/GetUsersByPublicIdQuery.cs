using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;

public record GetUsersByPublicIdQuery(
  Guid PermissionPublicId,
  int PageNumber = PaginationDefaults.DefaultPageNumber,
  int PageSize = PaginationDefaults.DefaultPageSize);
