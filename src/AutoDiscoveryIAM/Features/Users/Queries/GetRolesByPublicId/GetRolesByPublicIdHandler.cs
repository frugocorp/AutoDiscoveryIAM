using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;

public class GetRolesByPublicIdHandler(IGetRolesByPublicIdService service)
{
  private readonly IGetRolesByPublicIdService _service = service;

  public Task<OperationResult<IEnumerable<GetRolesByPublicIdResponseDto>>> Handle(GetRolesByPublicIdQuery query, CancellationToken cancellationToken)
  {
    return _service.ExecuteAsync(query.UserPublicId, cancellationToken);
  }
}
