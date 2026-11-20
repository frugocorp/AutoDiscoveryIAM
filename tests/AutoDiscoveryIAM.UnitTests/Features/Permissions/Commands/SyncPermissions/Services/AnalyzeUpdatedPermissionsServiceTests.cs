// -----------------------------------------------------------------------
// <copyright file="AnalyzeUpdatedPermissionsServiceTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.SyncPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Catalogs;
using System.Reflection;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions.Services;

/// <summary>
/// Tests unitarios para AnalyzeUpdatedPermissionsService (servicio orquestador).
/// </summary>
public class AnalyzeUpdatedPermissionsServiceTests
{
  private readonly IDiscoveryPermissionsService _mockDiscoveryService;
  private readonly ISyncPermissionsService _mockSyncService;
  private readonly IPermissionLogger _mockLogger;
  private readonly AnalyzeUpdatedPermissionsService _service;

  public AnalyzeUpdatedPermissionsServiceTests()
  {
    _mockDiscoveryService = Substitute.For<IDiscoveryPermissionsService>();
    _mockSyncService = Substitute.For<ISyncPermissionsService>();
    _mockLogger = Substitute.For<IPermissionLogger>();
    _service = new AnalyzeUpdatedPermissionsService(_mockDiscoveryService, _mockSyncService, _mockLogger);
  }

  [Theory]
  [InlineData(false, false)]
  [InlineData(true, false)]
  [InlineData(false, true)]
  [InlineData(true, true)]
  public async Task SyncPermissionsAsync_WithDifferentFlags_ShouldPassCorrectParameters(bool forceFullScan, bool dryRun)
  {
    // Preparar
    var discoveredPermissions = CreateSamplePermissionDescriptors(3);

    _mockDiscoveryService
        .DiscoverPermissionsAsync(forceFullScan, Arg.Any<bool>(), Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    _mockDiscoveryService
        .ValidateConsistency(discoveredPermissions)
        .Returns(OperationResult.Ok<IList<string>>(new List<string>()));

    var syncResponse = CreateSampleSyncResponse(dryRun);
    _mockSyncService
        .SyncPermissionsAsync(discoveredPermissions, dryRun, Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok(syncResponse));

    // Actuar
    var result = await _service.SyncPermissionsAsync(forceFullScan, dryRun, enableDetailedConsoleOutput: false, CancellationToken.None);

    // Verificar
    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.DryRunMode.Should().Be(dryRun);
    result.Data!.PermissionsDiscovered.Should().Be(3);

    // Verificar que los servicios fueron llamados con los par├ímetros correctos
    await _mockDiscoveryService.Received(1).DiscoverPermissionsAsync(forceFullScan, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    _mockDiscoveryService.Received(1).ValidateConsistency(discoveredPermissions);
    await _mockSyncService.Received(1).SyncPermissionsAsync(discoveredPermissions, dryRun, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenDiscoveryFails_ShouldReturnFailure()
  {
    // Preparar
    var errorInfo = ErrorInfo.Status500Unknown;
    _mockDiscoveryService
        .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
        .Returns(OperationResult.Fail<IList<PermissionDescriptor>>(errorInfo, PermissionErrorMessagesCatalog.DiscoveryServiceError));

    // Actuar
    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    // Verificar
    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(errorInfo);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.DiscoveryServiceError);

    // Verificar que sync no fue llamado
    await _mockSyncService.DidNotReceive().SyncPermissionsAsync(Arg.Any<IList<PermissionDescriptor>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenDiscoverySucceedsWithNullData_ShouldReturnFailure()
  {
    _mockDiscoveryService
      .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(null!));

    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.NoPermissionsDiscovered);
    await _mockSyncService.DidNotReceive().SyncPermissionsAsync(Arg.Any<IList<PermissionDescriptor>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenValidationFails_ShouldReturnFailure()
  {
    // Preparar
    var discoveredPermissions = CreateSamplePermissionDescriptors(2);
    var validationErrors = new List<string> { "Error 1", "Error 2" };

    _mockDiscoveryService
        .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    _mockDiscoveryService
        .ValidateConsistency(discoveredPermissions)
        .Returns(OperationResult.Fail<IList<string>>(ErrorInfo.Status500Unknown, PermissionErrorMessagesCatalog.ValidationErrors));

    // Actuar
    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    // Verificar
    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);

    // Verificar que sync no fue llamado
    await _mockSyncService.DidNotReceive().SyncPermissionsAsync(Arg.Any<IList<PermissionDescriptor>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenValidationFailsWithoutErrorMessage_ShouldUseFallbackMessage()
  {
    var discoveredPermissions = CreateSamplePermissionDescriptors(2);

    _mockDiscoveryService
      .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    _mockDiscoveryService
      .ValidateConsistency(discoveredPermissions)
      .Returns(OperationResult.Fail<IList<string>>(ErrorInfo.Status400Validation, null!));

    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.ValidationConsistencyError);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenValidationFailsWithoutErrorInfo_ShouldUseFallbackErrorInfo()
  {
    var discoveredPermissions = CreateSamplePermissionDescriptors(2);

    _mockDiscoveryService
      .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    var validationResult = CreateOperationResult<IList<string>>(false, new List<string>(), null, null);
    _mockDiscoveryService
      .ValidateConsistency(discoveredPermissions)
      .Returns(validationResult);

    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.ValidationConsistencyError);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenSyncFails_ShouldReturnFailure()
  {
    // Preparar
    var discoveredPermissions = CreateSamplePermissionDescriptors(2);

    _mockDiscoveryService
        .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    _mockDiscoveryService
        .ValidateConsistency(discoveredPermissions)
        .Returns(OperationResult.Ok<IList<string>>(new List<string>()));

    var errorInfo = ErrorInfo.Status500Unknown;
    _mockSyncService
        .SyncPermissionsAsync(discoveredPermissions, false, Arg.Any<CancellationToken>())
        .Returns(OperationResult.Fail<SyncPermissionsResponseDto>(errorInfo, PermissionErrorMessagesCatalog.SyncServiceError));

    // Actuar
    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    // Verificar
    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(errorInfo);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.SyncServiceError);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenSyncSucceedsWithNullData_ShouldReturnFailure()
  {
    var discoveredPermissions = CreateSamplePermissionDescriptors(2);

    _mockDiscoveryService
      .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    _mockDiscoveryService
      .ValidateConsistency(discoveredPermissions)
      .Returns(OperationResult.Ok<IList<string>>(new List<string>()));

    _mockSyncService
      .SyncPermissionsAsync(discoveredPermissions, false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<SyncPermissionsResponseDto>(null!));

    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.SyncDatabaseError);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenSyncFailsWithoutErrorMessage_ShouldUseFallbackMessage()
  {
    var discoveredPermissions = CreateSamplePermissionDescriptors(2);

    _mockDiscoveryService
      .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    _mockDiscoveryService
      .ValidateConsistency(discoveredPermissions)
      .Returns(OperationResult.Ok<IList<string>>(new List<string>()));

    _mockSyncService
      .SyncPermissionsAsync(discoveredPermissions, false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<SyncPermissionsResponseDto>(ErrorInfo.Status500Unknown, null!));

    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.SyncDatabaseError);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenSyncFailsWithoutErrorInfo_ShouldUseFallbackErrorInfo()
  {
    var discoveredPermissions = CreateSamplePermissionDescriptors(2);

    _mockDiscoveryService
      .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(discoveredPermissions));

    _mockDiscoveryService
      .ValidateConsistency(discoveredPermissions)
      .Returns(OperationResult.Ok<IList<string>>(new List<string>()));

    var syncResult = CreateOperationResult<SyncPermissionsResponseDto>(false, null, null, null);
    _mockSyncService
      .SyncPermissionsAsync(discoveredPermissions, false, Arg.Any<CancellationToken>())
      .Returns(syncResult);

    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.SyncDatabaseError);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithEmptyDiscoveredPermissions_ShouldHandleGracefully()
  {
    // Preparar
    var emptyPermissions = new List<PermissionDescriptor>();

    _mockDiscoveryService
        .DiscoverPermissionsAsync(false, Arg.Any<bool>(), Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok<IList<PermissionDescriptor>>(emptyPermissions));

    _mockDiscoveryService
        .ValidateConsistency(emptyPermissions)
        .Returns(OperationResult.Ok<IList<string>>(new List<string>()));

    var syncResponse = CreateSampleSyncResponse(false, 0, 0, 0, 0, 0);
    _mockSyncService
        .SyncPermissionsAsync(emptyPermissions, false, Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok(syncResponse));

    // Actuar
    var result = await _service.SyncPermissionsAsync(false, false, enableDetailedConsoleOutput: false, CancellationToken.None);

    // Verificar - El servicio debe tratar permisos vac├¡os como una condici├│n de error
    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.NoPermissionsDiscovered);
  }

  [Fact]
  public void Constructor_WithNullDiscoveryService_ShouldThrowArgumentNullException()
  {
    // Actuar y Verificar
    ((Action)(() =>
      _ = new AnalyzeUpdatedPermissionsService(null!, _mockSyncService, _mockLogger))).Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WithNullSyncService_ShouldThrowArgumentNullException()
  {
    // Actuar y Verificar
    ((Action)(() =>
      _ = new AnalyzeUpdatedPermissionsService(_mockDiscoveryService, null!, _mockLogger))).Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
  {
    ((Action)(() =>
      _ = new AnalyzeUpdatedPermissionsService(_mockDiscoveryService, _mockSyncService, null!))).Should().Throw<ArgumentNullException>();
  }

  #region Helper Methods

  private static List<PermissionDescriptor> CreateSamplePermissionDescriptors(int count)
  {
    var descriptors = new List<PermissionDescriptor>();
    for (int i = 1; i <= count; i++)
    {
      descriptors.Add(new PermissionDescriptor
      {
        Key = $"test.permission_{i}",
        Feature = "Test",
        Action = $"permission_{i}",
        Description = $"Test permission {i}",
        CqrsType = "Command",
        SourceFilePath = $"/test/TestEndpoint{i}.cs",
        EndpointMethodName = $"MapTestEndpoint{i}",
        HttpMethods = new List<string> { "POST" },
        Routes = new List<string> { $"/test/{i}" },
        IsObsolete = false,
        DiscoveredAt = DateTime.UtcNow,
        ContentHash = $"hash{i}",
        ValidationErrors = new List<string>(),
        ValidationWarnings = new List<string>(),
      });
    }
    return descriptors;
  }

  private static SyncPermissionsResponseDto CreateSampleSyncResponse(
      bool dryRun,
      int created = 2,
      int updated = 1,
      int obsoleted = 0,
      int recovered = 0,
      long executionTimeMs = 500)
  {
    return new SyncPermissionsResponseDto(
        PermissionsCreated: created,
        PermissionsUpdated: updated,
        PermissionsObsoleted: obsoleted,
        PermissionsRecovered: recovered,
        ExecutionTimeMs: executionTimeMs,
        DatabaseOperations: new DatabaseOperationStatistics(
            TotalQueriesExecuted: 5,
            InsertOperations: created,
            UpdateOperations: updated,
            SoftDeleteOperations: obsoleted,
            RecoveryOperations: recovered,
            TransactionDurationMs: executionTimeMs),
        ProcessingErrors: new List<string>(),
        ProcessingWarnings: new List<string>(),
        DryRunMode: dryRun);
  }

  private static OperationResult<T> CreateOperationResult<T>(bool success, T? data, string? error, ErrorInfo? errorInfo)
  {
    var ctor = typeof(OperationResult<T>).GetConstructor(
      BindingFlags.Instance | BindingFlags.NonPublic,
      binder: null,
      [typeof(bool), typeof(T), typeof(string), typeof(ErrorInfo)],
      modifiers: null)!;

    return (OperationResult<T>)ctor.Invoke([success, data, error, errorInfo]);
  }

  #endregion
}
