using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;

public class GetRolesByPublicIdHandler(IGetRolesByPublicIdService service)
{
    private readonly IGetRolesByPublicIdService _service = service;

    public Task<OperationResult<IEnumerable<GetRolesByPublicIdResponseDto>>> Handle(GetRolesByPublicIdQuery query, CancellationToken cancellationToken)
      => _service.ExecuteAsync(query.PermissionPublicId, cancellationToken);
}
