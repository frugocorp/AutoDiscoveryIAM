// -----------------------------------------------------------------------
// <copyright file="GetRolesHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;

public class GetRolesHandler(IGetRolesRepository repository, IRoleLogger logger)
{
  private readonly IGetRolesRepository _repository = repository;
  private readonly IRoleLogger _logger = logger;

  public async Task<OperationResult<PagedResultDto<GetRolesResponseDto>>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(query);

    var (pageNumber, pageSize) = PaginationDefaults.Clamp(query.PageNumber, query.PageSize);
    var (roles, totalCount) = await _repository.GetRoles(pageNumber, pageSize);
    _logger.LogRolesListed(roles.Count);
    var dtoList = roles.Select(r => new GetRolesResponseDto(
        r.PublicId,
        r.Name,
        r.Description,
        r.IsActive,
        r.CreatedAt,
        r.UpdatedAt,
        r.DeletedAt,
        r.IsDeleted));
    var pagedResult = new PagedResultDto<GetRolesResponseDto>(dtoList, pageNumber, pageSize, totalCount);
    return OperationResult.Ok(pagedResult);
  }
}
