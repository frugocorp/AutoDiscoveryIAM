using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using TuProyecto.Features.UserProfiles.Models.Catalogs;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile.Docs;

/// <summary>
/// Requiere el paquete de OpenAPI nativo (ya incluido en el SDK Web de .NET) y AddOpenApi(...)
/// configurado en Program.cs -- ver "Extras opcionales" en el README de AutoDiscoveryIAM. Si no vas
/// a documentar la API con OpenAPI, borra este archivo y la carpeta Docs/ completa; no es requerido
/// por el resto del slice.
/// </summary>
public sealed class CreateUserProfileOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status201Created;

  protected override string SuccessDescription =>
    "Perfil creado. La respuesta incluye los datos guardados, incluido el PublicId del perfil y del usuario dueño.";

  protected override object SuccessExample { get; } = new ApiResponse<CreateUserProfileResponseDto>(
    true,
    new CreateUserProfileResponseDto(
      Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
      "Ingeniera de software",
      "Le apasiona el diseño de APIs y la arquitectura limpia.",
      MaritalStatusCatalog.Casado),
    null,
    null);

  protected override object RequestExample { get; } = new CreateUserProfileRequestDto(
    UserPublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
    Occupation: "Ingeniera de software",
    Biography: "Le apasiona el diseño de APIs y la arquitectura limpia.",
    MaritalStatus: MaritalStatusCatalog.Casado);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };
}
