// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsHandlerTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using System.Reflection;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Tests unitarios para SyncPermissionsHandler.
/// </summary>
public class SyncPermissionsHandlerTests
{
  private readonly IAnalyzeUpdatedPermissionsService _mockAnalyzeService;
  private readonly IPermissionLogger _mockLogger;
  private readonly IAutoDiscoveryIAMUnitOfWork _mockUnitOfWork;
  private readonly SyncPermissionsHandler _handler;

  public SyncPermissionsHandlerTests()
  {
    _mockAnalyzeService = Substitute.For<IAnalyzeUpdatedPermissionsService>();
    _mockLogger = Substitute.For<IPermissionLogger>();
    _mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    _handler = new SyncPermissionsHandler(_mockAnalyzeService, _mockLogger, _mockUnitOfWork);
  }

  [Fact]
  public async Task Handle_WithValidCommand_ShouldReturnSuccess()
  {
    // Preparar
    var request = new SyncPermissionsRequestDto(
        ForceFullSync: false,
        DryRun: false);
    var command = new SyncPermissionsCommand(request);

    var analyzeResponse = new AnalyzeUpdatedPermissionsResponseDto(
        PermissionsDiscovered: 5,
        PermissionsCreated: 2,
        PermissionsUpdated: 1,
        PermissionsObsoleted: 0,
        PermissionsRecovered: 0,
        TotalExecutionTimeMs: 1500,
        DiscoveryExecutionTimeMs: 800,
        SyncExecutionTimeMs: 700,
        FilesScanned: 10,
        FeaturesAnalyzed: new List<string> { "Users", "Posts" },
        DryRunMode: false);

    _mockAnalyzeService
        .SyncPermissionsAsync(false, false, false, Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok(analyzeResponse));

    // Actuar
    var result = await _handler.Handle(command, CancellationToken.None);

    // Verificar
    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.OrchestrationResult.PermissionsDiscovered.Should().Be(5);
    result.Data!.OrchestrationResult.PermissionsCreated.Should().Be(2);
    result.Data!.OrchestrationResult.PermissionsUpdated.Should().Be(1);    // Verificar que el servicio fue llamado
    await _mockAnalyzeService.Received(1).SyncPermissionsAsync(false, false, false, Arg.Any<CancellationToken>());

    // Verificar que AutoDiscoveryIAMUnitOfWork fue llamado para modo no-DryRun
    await _mockUnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

    // Verificar logging
    _mockLogger.Received(1).LogPermissionsSyncStarted(false, false);
    _mockLogger.Received(1).LogPermissionsSyncCompleted(5, 1500, false);
    _mockLogger.Received(1).LogPermissionsSyncPersisted(5);
  }

  [Fact]
  public async Task Handle_WithDryRunTrue_ShouldNotPersistChanges()
  {
    // Preparar
    var request = new SyncPermissionsRequestDto(
        ForceFullSync: false,
        DryRun: true);
    var command = new SyncPermissionsCommand(request);

    var analyzeResponse = new AnalyzeUpdatedPermissionsResponseDto(
        PermissionsDiscovered: 3,
        PermissionsCreated: 1,
        PermissionsUpdated: 0,
        PermissionsObsoleted: 0,
        PermissionsRecovered: 0,
        TotalExecutionTimeMs: 1000,
        DiscoveryExecutionTimeMs: 600,
        SyncExecutionTimeMs: 400,
        FilesScanned: 8,
        FeaturesAnalyzed: new List<string> { "Auth" },
        DryRunMode: true);

    _mockAnalyzeService
        .SyncPermissionsAsync(false, true, false, Arg.Any<CancellationToken>())
        .Returns(OperationResult.Ok(analyzeResponse));

    // Actuar
    var result = await _handler.Handle(command, CancellationToken.None);

    // Verificar
    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.OrchestrationResult.DryRunMode.Should().BeTrue();    // Verificar que AutoDiscoveryIAMUnitOfWork NO fue llamado para DryRun
    await _mockUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

    // Verificar logging
    _mockLogger.Received(1).LogPermissionsSyncStarted(false, true);
    _mockLogger.Received(1).LogPermissionsSyncCompleted(3, 1000, true);
    _mockLogger.DidNotReceive().LogPermissionsSyncPersisted(Arg.Any<int>());
  }

  [Fact]
  public async Task Handle_ShouldForwardAllFlagsToAnalyzeService()
  {
    var request = new SyncPermissionsRequestDto(
        ForceFullSync: true,
        DryRun: true,
        EnableDetailedConsoleOutput: true);
    var command = new SyncPermissionsCommand(request);

    var analyzeResponse = new AnalyzeUpdatedPermissionsResponseDto(
        PermissionsDiscovered: 2,
        PermissionsCreated: 0,
        PermissionsUpdated: 0,
        PermissionsObsoleted: 0,
        PermissionsRecovered: 0,
        TotalExecutionTimeMs: 10,
        DiscoveryExecutionTimeMs: 5,
        SyncExecutionTimeMs: 5,
        FilesScanned: 1,
        FeaturesAnalyzed: new List<string> { "Users" },
        DryRunMode: true);

    _mockAnalyzeService
      .SyncPermissionsAsync(true, true, true, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Ok(analyzeResponse));

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    await _mockAnalyzeService.Received(1).SyncPermissionsAsync(true, true, true, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenAnalyzeServiceFails_ShouldReturnFailure()
  {
    // Preparar
    var request = new SyncPermissionsRequestDto(
        ForceFullSync: false,
        DryRun: false);
    var command = new SyncPermissionsCommand(request);

    var errorInfo = ErrorInfo.Status500Unknown;
    _mockAnalyzeService
        .SyncPermissionsAsync(false, false, false, Arg.Any<CancellationToken>())
        .Returns(OperationResult.Fail<AnalyzeUpdatedPermissionsResponseDto>(errorInfo, PermissionErrorMessagesCatalog.AnalysisServiceError));

    // Actuar
    var result = await _handler.Handle(command, CancellationToken.None);

    // Verificar
    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(errorInfo);
    (result.Error ?? string.Empty).Should().Contain(PermissionErrorMessagesCatalog.AnalysisServiceError);

    // Verificar que AutoDiscoveryIAMUnitOfWork NO fue llamado
    await _mockUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

    // Verificar logging de error - usando el m├®todo espec├¡fico nuevo
    _mockLogger.Received(1).LogPermissionsSyncFailed(PermissionErrorMessagesCatalog.AnalysisServiceError);
  }

  [Fact]
  public async Task Handle_WhenAnalyzeServiceFailsWithoutErrorMessage_ShouldUseFallbackMessage()
  {
    var request = new SyncPermissionsRequestDto(
        ForceFullSync: false,
        DryRun: false);
    var command = new SyncPermissionsCommand(request);

    _mockAnalyzeService
      .SyncPermissionsAsync(false, false, false, Arg.Any<CancellationToken>())
      .Returns(OperationResult.Fail<AnalyzeUpdatedPermissionsResponseDto>(ErrorInfo.Status500Unknown, null!));

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    (result.Error ?? string.Empty).Should().Contain("Error desconocido en la orquestación");
    _mockLogger.Received(1).LogPermissionsSyncFailed("Error desconocido");
    await _mockUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenAnalyzeServiceFailsWithoutErrorInfo_ShouldUseFallbackErrorInfo()
  {
    var request = new SyncPermissionsRequestDto(
        ForceFullSync: false,
        DryRun: false);
    var command = new SyncPermissionsCommand(request);

    var failedResult = CreateOperationResult<AnalyzeUpdatedPermissionsResponseDto>(false, null, null, null);
    _mockAnalyzeService
      .SyncPermissionsAsync(false, false, false, Arg.Any<CancellationToken>())
      .Returns(failedResult);

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    (result.Error ?? string.Empty).Should().Contain("Error desconocido en la orquestación");
    _mockLogger.Received(1).LogPermissionsSyncFailed("Error desconocido");
  }

  [Fact]
  public void Constructor_WithNullAnalyzeService_ShouldThrowArgumentNullException()
  {
    // Actuar y Verificar
    ((Action)(() =>
      _ = new SyncPermissionsHandler(null!, _mockLogger, _mockUnitOfWork))).Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
  {
    // Actuar y Verificar
    ((Action)(() =>
      _ = new SyncPermissionsHandler(_mockAnalyzeService, null!, _mockUnitOfWork))).Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
  {
    // Actuar y Verificar
    ((Action)(() =>
      _ = new SyncPermissionsHandler(_mockAnalyzeService, _mockLogger, null!))).Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public async Task Handle_WithNullCommand_ShouldThrowArgumentNullException()
  {
    // Actuar y Verificar
    await ((Func<Task>)(() =>
      _handler.Handle(null!, CancellationToken.None))).Should().ThrowAsync<ArgumentNullException>();
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
}
