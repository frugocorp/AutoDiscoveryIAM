using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;

public class GetUsersByPublicIdHandler(IGetUsersByPublicIdService service)
{
    private readonly IGetUsersByPublicIdService _service = service;

    public Task<OperationResult<IEnumerable<GetUsersByPublicIdResponseDto>>> Handle(GetUsersByPublicIdQuery query, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = PaginationDefaults.Clamp(query.PageNumber, query.PageSize);
        return _service.ExecuteAsync(query.PermissionPublicId, pageNumber, pageSize, cancellationToken);
    }
}
