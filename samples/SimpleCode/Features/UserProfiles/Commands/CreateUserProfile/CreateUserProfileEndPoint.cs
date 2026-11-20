using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using TuProyecto.Features.UserProfiles.Commands.CreateUserProfile.Docs;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public static class CreateUserProfileEndPoint
{
  public static void MapCreateUserProfileEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/user-profiles", async (
        CreateUserProfileRequestDto dto,
        CreateUserProfileHandler handler,
        CreateUserProfileCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new CreateUserProfileCommand(dto.UserPublicId, dto.Occupation, dto.Biography, dto.MaritalStatus);

      // Requiere el paquete FluentValidation -- si no lo instalaste, quita este bloque y
      // CreateUserProfileCommandValidator.cs (ver "Extras opcionales" en el README de AutoDiscoveryIAM).
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var invalidResponse = new CreateUserProfileResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(invalidResponse);
      }

      var result = await handler.Handle(command, cancellationToken);

      if (result.Success && result.Data != null)
      {
        var response = new CreateUserProfileResponse(true, result.Data);
        return Results.Created($"/user-profiles/{result.Data.PublicId}", response);
      }

      var errorResponse = new CreateUserProfileResponse(false, null, result.Error, result.ErrorInfo);
      return Results.Json(errorResponse, statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .WithName("CreateUserProfile")
    .WithTags("UserProfiles")
    .WithSummary("Crea el perfil extendido de un usuario existente.")
    .Accepts<CreateUserProfileRequestDto>("application/json")
    .Produces<CreateUserProfileResponse>(StatusCodes.Status201Created, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    // [PermissionsNotRequired] es solo para que este ejemplo funcione sin sembrar un Permission/Role
    // propio primero -- en tu app real, lo normal es NO poner ningún atributo (RBAC estándar) y
    // asignar el permiso "userprofiles.create_user_profile" (creado por POST /permissions/sync) al rol
    // que corresponda. Ver "Autorización con niveles de acceso" en el README de AutoDiscoveryIAM
    // para las demás opciones.
    .WithMetadata(new PermissionsNotRequiredAttribute("Ejemplo ilustrativo -- quitar en una app real"))
    // La siguiente línea requiere tener OpenAPI configurado (ver "Extras opcionales" en el README)
    // -- si no lo usas, quita esta línea y la carpeta Docs/ completa.
    .WithMetadata(new CreateUserProfileOperationDocs());
  }
}
