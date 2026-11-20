using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;

public class GetMyPermissionsHandler(IAuthContextService authContextService, IGetMyPermissionsService service)
{
  private readonly IAuthContextService _authContextService = authContextService;
  private readonly IGetMyPermissionsService _service = service;

  public Task<OperationResult<IEnumerable<GetMyPermissionsResponseDto>>> Handle(GetMyPermissionsQuery query, CancellationToken cancellationToken)
  {
    _ = query ?? throw new ArgumentNullException(nameof(query));

    return _service.ExecuteAsync(_authContextService.GetCurrentUserPublicId(), cancellationToken);
  }
}
