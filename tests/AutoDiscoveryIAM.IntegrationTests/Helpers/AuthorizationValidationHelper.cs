#pragma warning disable CS8602
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

/// <summary>
/// Helper para validaciones de autorización en tests.
/// </summary>
public static class AuthorizationValidationHelper
{
  /// <summary>
  /// Valida que la respuesta contenga el error de autenticación requerida con las 3 propiedades completas.
  /// </summary>
  public static void ValidateAuthenticationRequired(
      HttpResponseMessage response,
      ApiResponse<object>? apiResponse,
      string? customMessage = null)
  {
    apiResponse.Should().NotBeNull();

    // ✅ SIN HARDCODING - Usando ErrorInfo de la excepción real
    var expectedErrorInfo = new AuthenticationRequiredException("").ErrorInfo;

    // 1. StatusCode
    response.StatusCode.Should().Be((HttpStatusCode)expectedErrorInfo.StatusCode);

    // 2. AuthCode
    apiResponse.ErrorInfo?.Key.Should().Be(expectedErrorInfo.Key);

    // 3. Message
    var expectedMessage = customMessage ?? AuthorizationValidationTypes.Messages.AuthenticationRequired;
    apiResponse.Error.Should().Be(expectedMessage);
  }

  /// <summary>
  /// Valida que la respuesta indique acceso denegado por permisos insuficientes.
  /// </summary>
  public static void ValidateInsufficientPermissions(
      HttpResponseMessage response,
      ApiResponse<object>? apiResponse,
      string permissionKey,
      string? customMessage = null)
  {
    apiResponse.Should().NotBeNull();

    // ✅ SIN HARDCODING - Usando ErrorInfo de la excepción real
    var expectedErrorInfo = new InsufficientPermissionsException("").ErrorInfo;

    // 1. StatusCode
    response.StatusCode.Should().Be((HttpStatusCode)expectedErrorInfo.StatusCode);

    // 2. AuthCode
    apiResponse.ErrorInfo?.Key.Should().Be(expectedErrorInfo.Key);

    // 3. Message
    var expectedMessage = customMessage ?? $"{AuthorizationValidationTypes.Messages.InsufficientPermissions}. Requerido: {permissionKey}";
    apiResponse.Error.Should().Be(expectedMessage);
  }

  /// <summary>
  /// Valida que la respuesta indique acceso denegado por requerir acceso de super admin.
  /// </summary>
  public static void ValidateSuperAdminAccessRequired(
      HttpResponseMessage response,
      ApiResponse<object>? apiResponse,
      string? reason = null,
      string? customMessage = null)
  {
    apiResponse.Should().NotBeNull();

    // ✅ SIN HARDCODING - Usando ErrorInfo de la excepción real
    var expectedErrorInfo = new SuperAdminAccessRequiredException("").ErrorInfo;

    // 1. StatusCode
    response.StatusCode.Should().Be((HttpStatusCode)expectedErrorInfo.StatusCode);

    // 2. AuthCode
    apiResponse.ErrorInfo?.Key.Should().Be(expectedErrorInfo.Key);

    // 3. Message
    var expectedMessage = customMessage ?? (string.IsNullOrWhiteSpace(reason)
        ? AuthorizationValidationTypes.Messages.SuperAdminAccessRequired
        : $"{AuthorizationValidationTypes.Messages.SuperAdminAccessRequired}: {reason}");
    apiResponse.Error.Should().Be(expectedMessage);
  }

  /// <summary>
  /// Valida que la respuesta indique acceso denegado por requerir acceso de desarrollo.
  /// </summary>
  public static void ValidateDevelopmentAccessRequired(
      HttpResponseMessage response,
      ApiResponse<object>? apiResponse,
      string? reason = null,
      string? customMessage = null)
  {
    apiResponse.Should().NotBeNull();

    // ✅ SIN HARDCODING - Usando ErrorInfo de la excepción real
    var expectedErrorInfo = new DevelopmentAccessRequiredException("").ErrorInfo;

    // 1. StatusCode
    response.StatusCode.Should().Be((HttpStatusCode)expectedErrorInfo.StatusCode);

    // 2. AuthCode
    apiResponse.ErrorInfo?.Key.Should().Be(expectedErrorInfo.Key);

    // 3. Message
    var expectedMessage = customMessage ?? (string.IsNullOrWhiteSpace(reason)
        ? AuthorizationValidationTypes.Messages.DevelopmentAccessRequired
        : $"{AuthorizationValidationTypes.Messages.DevelopmentAccessRequired}: {reason}");
    apiResponse.Error.Should().Be(expectedMessage);
  }

  /// <summary>
  /// Valida que la respuesta indique acceso exitoso (sin errores de autorización).
  /// </summary>
  public static void ValidateSuccessfulAccess(HttpResponseMessage response, ApiResponse<object>? apiResponse)
  {
    // No debe ser error de autorización
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

    // Si hay error, no debe ser de autorización
    if (apiResponse?.ErrorInfo?.Key != null)
    {
      apiResponse.ErrorInfo.Key.StartsWith("AUTH_", StringComparison.Ordinal).Should().BeFalse(
        $"La respuesta no debe contener error de autorización, pero obtuvo: {apiResponse.ErrorInfo.Key}");
    }
  }

  /// <summary>
  /// Determina si un usuario debería tener acceso a un endpoint específico.
  /// </summary>
  public static bool ShouldUserHaveAccess(UserType userType, EndpointAuthorizationType endpointType)
  {
    return endpointType switch
    {
      EndpointAuthorizationType.PermissionsNotRequired => true, // Todos pueden acceder

      EndpointAuthorizationType.RequiresSuAdminAccess =>
          userType == UserType.SuAdmin || userType == UserType.Development,

      EndpointAuthorizationType.RequiresDevelopmentAccess =>
          userType == UserType.Development,

      EndpointAuthorizationType.RequiresRegularPermissions =>
          userType != UserType.Anonymous, // Todos excepto anonymous (puede fallar por permisos específicos)

      _ => throw new ArgumentOutOfRangeException(nameof(endpointType)),
    };
  }

  /// <summary>
  /// Valida la respuesta según las expectativas para un usuario y endpoint específicos.
  /// </summary>
  public static void ValidateExpectedResponse(
      HttpResponseMessage response,
      ApiResponse<object>? apiResponse,
      UserType userType,
      EndpointAuthorizationType endpointType,
      string? endpointKey = null,
      string? suAdminReason = null,
      string? developmentReason = null)
  {
    var shouldHaveAccess = ShouldUserHaveAccess(userType, endpointType);

    if (shouldHaveAccess)
    {
      ValidateSuccessfulAccess(response, apiResponse);
    }
    else
    {
      // Determinar qué tipo de error esperar
      switch (endpointType)
      {
        case EndpointAuthorizationType.RequiresSuAdminAccess:
          if (userType == UserType.Anonymous)
            ValidateAuthenticationRequired(response, apiResponse);
          else
            ValidateSuperAdminAccessRequired(response, apiResponse, suAdminReason);
          break;

        case EndpointAuthorizationType.RequiresDevelopmentAccess:
          if (userType == UserType.Anonymous)
            ValidateAuthenticationRequired(response, apiResponse);
          else
            ValidateDevelopmentAccessRequired(response, apiResponse, developmentReason);
          break;

        case EndpointAuthorizationType.RequiresRegularPermissions:
          if (userType == UserType.Anonymous)
            ValidateAuthenticationRequired(response, apiResponse);
          else
            ValidateInsufficientPermissions(response, apiResponse, endpointKey ?? "");
          break;

        default:
          throw new ArgumentOutOfRangeException(nameof(endpointType));
      }
    }
  }

  /// <summary>
  /// Valida que una respuesta HTTP siga el formato ApiResponse básico.
  /// NOTA: Para validaciones específicas de autorización, usar los métodos especializados
  /// (ValidateAuthenticationRequired, ValidateSuperAdminAccessRequired, etc.)
  /// </summary>
  public static async Task ValidateApiResponseFormat(
      HttpResponseMessage response,
      string expectedMessageContains)
  {
    // Validate Content-Type
    response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

    // Read and parse the response as ApiResponse<object>
    var responseBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
    responseBody.Should().NotBeNullOrEmpty();

    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>(
        cancellationToken: CancellationToken.None);
    apiResponse.Should().NotBeNull();

    // Validate ApiResponse structure
    apiResponse.Success.Should().BeFalse("Error responses debe tener Success=false");
    apiResponse.Data.Should().BeNull("Error responses debe tener Data=null");
    apiResponse.Error.Should().NotBeNullOrEmpty();
    apiResponse.Error.Should().Contain(expectedMessageContains, "El mensaje de error debe contener el texto esperado");
    apiResponse.Error.Should().NotBeNull("Error responses debe tener Error information");
  }
}



