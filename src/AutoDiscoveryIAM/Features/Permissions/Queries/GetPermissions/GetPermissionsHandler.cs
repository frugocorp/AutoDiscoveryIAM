// -----------------------------------------------------------------------
// <copyright file="GetPermissionsHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsHandler(IGetPermissionsRepository repository, IPermissionLogger logger)
{
  private readonly IGetPermissionsRepository _repository = repository;
  private readonly IPermissionLogger _logger = logger;

  public async Task<OperationResult<PagedResultDto<GetPermissionsResponseDto>>> Handle(GetPermissionsQuery query, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(query);

    var (pageNumber, pageSize) = PaginationDefaults.Clamp(query.PageNumber, query.PageSize);
    var (permissions, totalCount) = await _repository.GetPermissions(pageNumber, pageSize);
    _logger.LogPermissionsRetrieved(permissions.Count);
    var dtoList = permissions.Select(p => new GetPermissionsResponseDto(
        p.PublicId,
        p.Key,
        p.Description,
        p.Group,
        p.IsActive,
        p.Route,
        p.HttpMethod,
        p.CreatedAt,
        p.UpdatedAt));
    var pagedResult = new PagedResultDto<GetPermissionsResponseDto>(dtoList, pageNumber, pageSize, totalCount);
    return OperationResult.Ok(pagedResult);
  }
}
