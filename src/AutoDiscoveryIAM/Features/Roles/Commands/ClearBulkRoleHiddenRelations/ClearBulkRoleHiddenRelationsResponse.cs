using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public record ClearBulkRoleHiddenRelationsResponse : ApiResponse<ClearBulkRoleHiddenRelationsResponseDto>
{
    public ClearBulkRoleHiddenRelationsResponse(bool success, ClearBulkRoleHiddenRelationsResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
