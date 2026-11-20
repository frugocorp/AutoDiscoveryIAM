// -----------------------------------------------------------------------
// <copyright file="GetUsersHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

public class GetUsersHandler(IGetUsersRepository repository)
{
  private readonly IGetUsersRepository _repository = repository;

  public async Task<OperationResult<PagedResultDto<GetUsersResponseDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(query);

    var (pageNumber, pageSize) = PaginationDefaults.Clamp(query.PageNumber, query.PageSize);
    var (users, totalCount) = await _repository.GetUsersAsync(pageNumber, pageSize, cancellationToken);
    var dtoList = users.Select(GetUsersResponseDto.FromEntity);
    var pagedResult = new PagedResultDto<GetUsersResponseDto>(dtoList, pageNumber, pageSize, totalCount);
    return OperationResult.Ok(pagedResult);
  }
}
