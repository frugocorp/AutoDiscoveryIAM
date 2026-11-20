using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public class ClearBulkRoleHiddenRelationsHandler(IClearBulkRoleHiddenRelationsService service)
{
    private readonly IClearBulkRoleHiddenRelationsService _service = service;

    public Task<OperationResult<ClearBulkRoleHiddenRelationsResponseDto>> Handle(ClearBulkRoleHiddenRelationsCommand command, CancellationToken cancellationToken)
      => _service.ExecuteAsync(command.RolePublicId, cancellationToken);
}
