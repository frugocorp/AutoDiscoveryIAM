using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using TuProyecto.Features.UserProfiles.Infrastructure.Logging;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public class GetUserProfilesHandler(IGetUserProfilesRepository repository, IUserProfileLogger logger)
{
  private readonly IGetUserProfilesRepository _repository = repository;
  private readonly IUserProfileLogger _logger = logger;

  public async Task<OperationResult<PagedResultDto<GetUserProfilesResponseDto>>> Handle(GetUserProfilesQuery query, CancellationToken cancellationToken)
  {
    var (userProfiles, totalCount) = await _repository.GetUserProfiles(query.PageNumber, query.PageSize);
    _logger.LogUserProfilesListed(userProfiles.Count);

    var dtoList = userProfiles.Select(p => new GetUserProfilesResponseDto(
      p.PublicId, p.User.PublicId, p.User.Username, p.User.Email, p.Occupation, p.Biography, p.MaritalStatus, p.CreatedAt));
    var pagedResult = new PagedResultDto<GetUserProfilesResponseDto>(dtoList, query.PageNumber, query.PageSize, totalCount);
    return OperationResult.Ok(pagedResult);
  }
}
