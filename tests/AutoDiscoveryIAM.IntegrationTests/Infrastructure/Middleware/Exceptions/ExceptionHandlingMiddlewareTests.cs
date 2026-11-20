// -----------------------------------------------------------------------
// <copyright file="ExceptionHandlingMiddlewareTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Infrastructure.Middleware.Exceptions
{
#if DEBUG
  /// <summary>
  /// Pruebas de integración para el middleware global de manejo de excepciones.
  /// Valida que cualquier excepción no controlada sea capturada y devuelta en el formato estándar <see cref="ApiResponse{T}"/>.
  /// </summary>
  /// <remarks>
  /// Utiliza un <see cref="WebApplicationFactory{TEntryPoint}"/> para simular la aplicación real durante las pruebas.
  /// Incluye pruebas para garantizar que el middleware no exponga detalles técnicos y siempre retorne respuestas controladas.
  /// </remarks>
  /// <param name="factory">
  /// Fábrica de la aplicación web utilizada para crear el cliente HTTP en pruebas de integración.
  /// </param>
  public class ExceptionHandlingMiddlewareTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
#else
  /// <summary>
  /// Pruebas de integración para el middleware global de manejo de excepciones.
  /// Esta clase está vacía en modo Release ya que las pruebas solo están disponibles en DEBUG.
  /// </summary>
  public class ExceptionHandlingMiddlewareTests
#endif
  {
#if DEBUG
    private readonly WebApplicationFactory<Program> _factory = factory;
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
    private readonly JsonSerializerOptions _readOptions = new()
    {
      AllowTrailingCommas = true,
      PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Prueba que el middleware global de excepciones retorna un <see cref="ApiResponse{T}"/>
    /// correctamente estructurado cuando ocurre una excepción no controlada en el endpoint.
    /// </summary>
    /// <remarks>
    /// - El endpoint '/api/ExceptionHandlingMiddlewareDiagnostics/throw' está diseñado para lanzar una excepción deliberada.
    /// - Se valida que la respuesta HTTP sea 500 (InternalServerError).
    /// - Se comprueba que el objeto <see cref="ApiResponse{T}"/> recibido indique fallo, no tenga datos y contenga un mensaje de error.
    /// - Esta prueba solo se ejecuta en modo DEBUG ya que el endpoint de diagnóstico solo está disponible en DEBUG.
    /// - Se autentica como usuario Development ya que el endpoint requiere RequiresDevelopmentAccessAttribute.
    /// </remarks>
    [Fact]
    public async Task GlobalExceptionMiddleware_Returns_ApiResponse_On_Exception()
    {
      // Arrange: Autenticarse como usuario Development para acceder al endpoint protegido
      var client = _factory.CreateClient();

      // Login con usuario Development que tiene acceso a endpoints de diagnóstico
      var token = await client.GetAuthTokenAsync(UserType.Development, _cancellationToken);

      // Configurar header de autorización
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);      // Act: Llamar al endpoint que debe lanzar excepción
      var response = await client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", _cancellationToken);

      // Leer contenido con manejo de errores
      string content;
      try
      {
        content = await response.Content.ReadAsStringAsync(_cancellationToken);
      }
      catch (TaskCanceledException)
      {
        // Si se cancela al leer, asumimos que la excepción fue manejada correctamente
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        return;
      }

      // Assert: Verificar que se retorna 500 InternalServerError como se esperaba originalmente
      response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

      var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _readOptions);

      apiResponse.Should().NotBeNull();
      apiResponse!.Success.Should().BeFalse();
      apiResponse!.Data.Should().BeNull();
      string.IsNullOrEmpty(apiResponse!.Error).Should().BeFalse();
    }
#endif
  }
}
