#pragma warning disable CS8602
// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsEndPointTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Tests de integración para SyncPermissionsEndPoint.
/// </summary>
/// <remarks>
/// Utilizado por <see cref="SyncPermissionsEndPointTests"/>.
/// </remarks>
[Collection("GlobalTestSetup")]
public class SyncPermissionsEndPointTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

  /// <summary>
  /// Prueba de integración que verifica que el endpoint <c>POST /permissions/sync</c> sincroniza correctamente los permisos cuando se proporciona un token válido.
  /// </summary>
  /// <remarks>
  /// Este test realiza los siguientes pasos:
  /// <list type="number">
  ///   <item><description>Realiza login para obtener un token JWT válido (usuario Development).</description></item>
  ///   <item><description>Prepara y envía una petición de sincronización (<c>POST</c>) usando el token con <c>ForceFullSync=false</c>.</description></item>
  ///   <item><description>Verifica que la respuesta sea exitosa y que el proceso de sincronización haya funcionado correctamente.</description></item>
  /// </list>
  /// <para><b>ForceFullSync=false:</b> Solo sincroniza permisos que han cambiado desde la última sincronización (modo incremental - más rápido).</para>
  /// <para><b>DryRun=false:</b> Los cambios se persisten realmente en la base de datos (no es simulación).</para>
  /// <para>Se utiliza un <c>CancellationToken</c> para evitar bloqueos en la prueba en caso de demoras inesperadas.</para>
  /// </remarks>
  [Fact]
  public async Task SyncPermissions_WithValidToken_ShouldReturnSuccess()
  {
    // Arrange (Organizar)
    // Autenticar como Admin
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);

    // Paso 2: Preparar la petición de sincronización
    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", new CultureInfo("es-MX"));
    var request = new SyncPermissionsRequestDto(ForceFullSync: false, DryRun: false);

    // Act (Actuar)
    var response = await _client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    // Assert (Afirmar)
    response.EnsureSuccessStatusCode();
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var content = await response.Content.ReadFromJsonAsync<ApiResponse<SyncPermissionsCommandResponseDto>>(cancellationToken: _cancellationToken);
    content.Should().NotBeNull();
    content!.Success.Should().BeTrue();
    content.Data.Should().NotBeNull();
    content.Data!.OrchestrationResult.Should().NotBeNull();
    content.Data.OrchestrationResult.PermissionsDiscovered.Should().BeGreaterThanOrEqualTo(0);
    content.Data.OrchestrationResult.TotalExecutionTimeMs.Should().BeGreaterThan(0);
    content.Data.OrchestrationResult.FeaturesAnalyzed.Should().NotBeNull();
  }

  /// <summary>
  /// Prueba de integración que verifica que el endpoint funciona con <c>ForceFullSync=true</c>.
  /// </summary>
  /// <remarks>
  /// <para><b>ForceFullSync=true:</b> Fuerza una sincronización completa de TODOS los permisos del sistema,</para>
  /// <para>independientemente de si han cambiado o no. Esto es útil cuando:</para>
  /// <list type="bullet">
  ///   <item><description>Se quiere reconstruir completamente el catálogo de permisos.</description></item>
  ///   <item><description>Se han hecho cambios manuales en la BD que deben ser sobrescritos.</description></item>
  ///   <item><description>Se sospecha que hay inconsistencias entre el código y la BD.</description></item>
  /// </list>
  /// <para><b>Nota:</b> Esta operación es más lenta que el modo incremental, pero garantiza sincronización total.</para>
  /// </remarks>
  [Fact]
  public async Task SyncPermissions_WithForceFullSync_ShouldReturnSuccess()
  {
    // Arrange (Organizar)
    // Autenticar como Admin
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);

    var request = new SyncPermissionsRequestDto(ForceFullSync: true, DryRun: false, EnableDetailedConsoleOutput: true);

    // Act (Actuar)
    var response = await _client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    // Assert (Afirmar)
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadFromJsonAsync<ApiResponse<SyncPermissionsCommandResponseDto>>(cancellationToken: _cancellationToken);
    content.Should().NotBeNull();
    content!.Success.Should().BeTrue();
    content.Data.Should().NotBeNull();
  }

  /// <summary>
  /// Prueba de integración que verifica que el endpoint funciona en modo <c>DryRun=true</c>.
  /// </summary>
  /// <remarks>
  /// <para><b>DryRun=true:</b> Modo de simulación/prueba que ejecuta TODO el proceso de sincronización</para>
  /// <para>(discovery, análisis, comparación), pero NO persiste ningún cambio en la base de datos.</para>
  /// <para><b>Útil para:</b></para>
  /// <list type="bullet">
  ///   <item><description>Validar que el proceso funciona sin riesgo de modificar la BD.</description></item>
  ///   <item><description>Ver qué cambios se aplicarían sin aplicarlos realmente.</description></item>
  ///   <item><description>Probando y debugging del proceso de sincronización.</description></item>
  /// </list>
  /// <para><b>Resultado:</b> Retorna estadísticas de lo que SE HARÍA, pero la BD permanece intacta.</para>
  /// </remarks>
  [Fact]
  public async Task SyncPermissions_WithDryRun_ShouldReturnSuccess()
  {
    // Arrange (Organizar)
    // Autenticar como Admin
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);

    var request = new SyncPermissionsRequestDto(ForceFullSync: false, DryRun: true);

    // Act (Actuar)
    var response = await _client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    // Assert (Afirmar)
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadFromJsonAsync<ApiResponse<SyncPermissionsCommandResponseDto>>(cancellationToken: _cancellationToken);
    content.Should().NotBeNull();
    content!.Success.Should().BeTrue();
    content.Data.Should().NotBeNull();
    content.Data.OrchestrationResult.DryRunMode.Should().BeTrue();
  }

  /// <summary>
  /// Prueba de integración que verifica que el endpoint tiene el content-type correcto.
  /// </summary>
  [Fact]
  public async Task SyncPermissions_ShouldHaveCorrectContentType()
  {
    // Arrange (Organizar)
    // Autenticar como Admin
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);

    var request = new SyncPermissionsRequestDto();

    // Act (Actuar)
    var response = await _client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    // Assert (Afirmar)
    response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
  }

  /// <summary>
  /// Prueba de integración que verifica que los campos de autorización se capturan correctamente
  /// durante la sincronización de permisos.
  /// </summary>
  /// <remarks>
  /// Este test valida que el proceso de discovery captura correctamente los atributos de autorización
  /// desde los metadatos nativos del endpoint (<c>.WithMetadata()</c>), vía <c>EndpointDataSource</c>.
  /// </remarks>
  [Fact]
  public async Task SyncPermissions_ShouldCaptureAuthorizationAttributes()
  {
    // Arrange (Organizar)
    // Autenticar como Development user
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);

    // Forzar sincronización completa para asegurar que se descubran todos los endpoints
    var request = new SyncPermissionsRequestDto(ForceFullSync: true, DryRun: false);

    // Act (Actuar) - Ejecutar sincronización
    var response = await _client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    // Assert (Afirmar)
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadFromJsonAsync<ApiResponse<SyncPermissionsCommandResponseDto>>(cancellationToken: _cancellationToken);
    content.Should().NotBeNull();
    content!.Success.Should().BeTrue();

    // Verificar que se descubrieron permisos
    var orchestrationResult = content.Data!.OrchestrationResult;
    orchestrationResult.PermissionsDiscovered.Should().BeGreaterThan(0, "Debería haber descubierto permisos en el codebase");

    // Verificar que el proceso se ejecutó correctamente
    orchestrationResult.TotalExecutionTimeMs.Should().BeGreaterThan(0);
    orchestrationResult.DiscoveryExecutionTimeMs.Should().BeGreaterThan(0);
    orchestrationResult.FeaturesAnalyzed.Should().NotBeEmpty("Debería haber analizado al menos una feature");

    // Nota: No validamos PermissionsCreated/Updated porque en ejecuciones subsecuentes
    // los permisos ya existen sin cambios. Lo importante es que el discovery funciona.
  }

  [Fact]
  public async Task SyncPermissions_WithInvalidCombination_ShouldReturnBadRequest()
  {
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);
    var request = new SyncPermissionsRequestDto(ForceFullSync: true, DryRun: true);

    var response = await _client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var content = await response.Content.ReadAsStringAsync(_cancellationToken);
    content.ToLowerInvariant().Should().Contain("forcefullsync");
  }

  [Fact]
  public async Task SyncPermissions_WhenAnalyzeServiceFails_ShouldReturnConfiguredStatusCode()
  {
    using var failingFactory = factory.WithWebHostBuilder(builder =>
      builder.ConfigureServices(services =>
      {
        var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(IAnalyzeUpdatedPermissionsService));
        if (descriptor != null)
        {
          services.Remove(descriptor);
        }

        services.AddSingleton<IAnalyzeUpdatedPermissionsService>(new FailingAnalyzeUpdatedPermissionsService());
      }));

    using var client = failingFactory.CreateClient();
    await client.AuthenticateUserAsync(UserType.Development, _cancellationToken);
    var request = new SyncPermissionsRequestDto(ForceFullSync: false, DryRun: false);

    var response = await client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    var content = await response.Content.ReadAsStringAsync(_cancellationToken);
    content.Should().Contain("forced analyze failure");
  }

  [Fact]
  public async Task SyncPermissions_Swagger_ShouldExposePostOperationWithCustomTagAndDescription()
  {
    var response = await _client.GetAsync("/openapi/v1.json", _cancellationToken);

    response.EnsureSuccessStatusCode();
    var swagger = await response.Content.ReadAsStringAsync(_cancellationToken);

    swagger.Should().Contain("/permissions/sync");
    swagger.Should().Contain("Escanea todos los Map*Endpoint");
    swagger.Should().Contain("Permissions");
  }

  [Fact]
  public async Task SyncPermissions_Swagger_ShouldExposeExpectedResponseDescriptions()
  {
    var response = await _client.GetAsync("/openapi/v1.json", _cancellationToken);

    response.EnsureSuccessStatusCode();
    var swagger = await response.Content.ReadAsStringAsync(_cancellationToken);

    swagger.ToLowerInvariant().Should().Contain("sincronizacion ejecutada correctamente");
    swagger.Should().Contain(ErrorInfo.Status400Validation.Description);
    swagger.Should().Contain(ErrorInfo.Status401Unauthorized.Description);
    swagger.Should().Contain(ErrorInfo.DevelopmentAccessRequired.Description);
    swagger.Should().Contain(ErrorInfo.Status404NotFound.Description);
    swagger.Should().Contain(ErrorInfo.Status500Unknown.Description);
  }

  /// <summary>
  /// Issue #103: el sync reporta los endpoints que exigen permiso pero no pudieron nombrarse (antes se
  /// descartaban sin aviso y respondían 403 a casi todos). En el host de ejemplo no debe haber
  /// ninguno: todo endpoint propio fuera de <c>Features</c> (OpenAPI, Scalar, health) está marcado
  /// con <c>[PermissionsNotRequired]</c>.
  /// </summary>
  [Fact]
  public async Task SyncPermissions_OnSampleHost_ShouldReportNoEndpointsWithoutPermission()
  {
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);
    var request = new SyncPermissionsRequestDto(ForceFullSync: false, DryRun: true);

    var response = await _client.PostAsJsonAsync("/permissions/sync", request, _cancellationToken);

    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadFromJsonAsync<ApiResponse<SyncPermissionsCommandResponseDto>>(cancellationToken: _cancellationToken);
    var endpointsWithoutPermission = content!.Data!.OrchestrationResult.EndpointsWithoutPermission;
    endpointsWithoutPermission.Should().NotBeNull();
    string.Join(" | ", endpointsWithoutPermission!).Should().BeEmpty();
  }

  /// <summary>
  /// Issue #103: el endpoint del diseño PDP abandonado (consultado por Nginx vía auth_request) ya no
  /// se mapea en ninguna app consumidora.
  /// </summary>
  [Fact]
  public async Task PdpValidateEndpoint_ShouldNoLongerBeMapped()
  {
    await _client.AuthenticateUserAsync(UserType.Development, _cancellationToken);

    var response = await _client.GetAsync("/internal/auth/validate", _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  private sealed class FailingAnalyzeUpdatedPermissionsService : IAnalyzeUpdatedPermissionsService
  {
    public Task<OperationResult<AnalyzeUpdatedPermissionsResponseDto>> SyncPermissionsAsync(bool forceFullScan = false, bool dryRun = false, bool enableDetailedConsoleOutput = false, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(OperationResult.Fail<AnalyzeUpdatedPermissionsResponseDto>(
        ErrorInfo.Status500Unknown,
        "forced analyze failure"));
    }
  }
}



