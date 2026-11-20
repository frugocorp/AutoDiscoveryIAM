// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

public class GetUserByPublicIdHandler(IGetUserByPublicIdRepository repository, IUserLogger logger)
{
  private readonly IGetUserByPublicIdRepository _repository = repository;
  private readonly IUserLogger _logger = logger;

  public async Task<OperationResult<GetUserByPublicIdResponseDto>> Handle(GetUserByPublicIdQuery query, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(query);

    var user = await _repository.GetUserByPublicIdAsync(query.PublicId, cancellationToken);
    if (user == null)
    {
      _logger.LogUserNotFoundByPublicId(query.PublicId);
      return OperationResult.Fail<GetUserByPublicIdResponseDto>(
          ErrorInfo.Status404NotFound,
          $"No se encontró el usuario con PublicId {query.PublicId}");
    }

    _logger.LogUserRetrieved(user.Id);
    return OperationResult.Ok(GetUserByPublicIdResponseDto.FromEntity(user));
  }
}
