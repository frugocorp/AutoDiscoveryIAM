using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using TuProyecto.Features.UserProfiles.Models.Catalogs;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles.Docs;

/// <summary>
/// Requiere OpenAPI configurado -- ver la nota en Docs/CreateUserProfileOperationDocs.cs. Borra
/// este archivo (y la carpeta Docs/) si no vas a documentar la API con OpenAPI.
/// </summary>
public sealed class GetUserProfilesOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Lista paginada de perfiles de usuario, con el username/email de cada usuario dueño.";

  protected override object SuccessExample { get; } = new ApiResponse<PagedResultDto<GetUserProfilesResponseDto>>(
    true,
    new PagedResultDto<GetUserProfilesResponseDto>(
      [new GetUserProfilesResponseDto(
        Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
        Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
        "aruiz",
        "aruiz@example.com",
        "Ingeniera de software",
        "Le apasiona el diseño de APIs y la arquitectura limpia.",
        MaritalStatusCatalog.Casado,
        DateTime.UtcNow)],
      1,
      20,
      1),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
  };
}
