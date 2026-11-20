using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;

public class GetPermissionsByPublicIdHandler(IGetPermissionsByPublicIdService service)
{
  private readonly IGetPermissionsByPublicIdService _service = service;

  public Task<OperationResult<IEnumerable<GetPermissionsByPublicIdResponseDto>>> Handle(GetPermissionsByPublicIdQuery query, CancellationToken cancellationToken)
  {
    return _service.ExecuteAsync(query.UserPublicId, cancellationToken);
  }
}
