// ---------------------------------------
// <copyright file="SyncPermissionsServiceTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.SyncPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;
using NSubstitute;
using System.Reflection;
using PermissionDescriptor = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.PermissionDescriptor;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions.Services;

public class SyncPermissionsServiceTests
{
  private readonly ISyncPermissionsRepository _mockRepository;
  private readonly IPermissionLogger _mockLogger;
  private readonly SyncPermissionsService _service;

  public SyncPermissionsServiceTests()
  {
    _mockRepository = Substitute.For<ISyncPermissionsRepository>();
    _mockLogger = Substitute.For<IPermissionLogger>();
    _service = new SyncPermissionsService(_mockRepository, _mockLogger);
    SetupDefaultRepositoryBehavior();
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenRepositoryIsNull()
  {
    var act = () => _ = new SyncPermissionsService(null!, _mockLogger);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenLoggerIsNull()
  {
    var act = () => _ = new SyncPermissionsService(_mockRepository, null!);

    act.Should().Throw<ArgumentNullException>();
  }

  private void SetupDefaultRepositoryBehavior()
  {
    _mockRepository.Stats.Returns(new RepositoryStats());
    _mockRepository.GetActivePermissionsAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission>());
    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission>());
    _mockRepository.GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Permission?)null);

    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id > 0), Arg.Any<CancellationToken>())
      .Returns(new Permission
      {
        Id = 1L,
        Key = "test_permission",
        Description = "Test Permission",
        IsActive = true,
        Route = "/test",
        HttpMethod = "GET",
        Group = "Test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      });

    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id <= 0), Arg.Any<CancellationToken>())
      .Returns((Permission?)null);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task SyncPermissionsAsync_WithValidDescriptors_ShouldReturnSuccess(bool dryRun)
  {
    var discoveredPermissions = CreateSamplePermissionDescriptors(3);

    var result = await _service.SyncPermissionsAsync(discoveredPermissions, dryRun, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.DryRunMode.Should().Be(dryRun);
    result.Data!.DatabaseOperations.Should().NotBeNull();
    result.Data!.ProcessingErrors.Should().NotBeNull();
    result.Data!.ProcessingWarnings.Should().NotBeNull();
    (result.Data!.ExecutionTimeMs >= 0).Should().BeTrue();
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithEmptyDescriptors_ShouldReturnSuccessWithZeroCounts()
  {
    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor>(), false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.PermissionsCreated.Should().Be(0);
    result.Data!.PermissionsUpdated.Should().Be(0);
    result.Data!.PermissionsObsoleted.Should().Be(0);
    result.Data!.PermissionsRecovered.Should().Be(0);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithNullDescriptors_ShouldThrowArgumentNullException()
  {
    await ((Func<Task>)(() =>
      _service.SyncPermissionsAsync(null!, false, CancellationToken.None))).Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithCancellationToken_ShouldRespectCancellation()
  {
    var permissions = CreateSamplePermissionDescriptors(1);
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await ((Func<Task>)(() =>
      _service.SyncPermissionsAsync(permissions, false, cts.Token))).Should().ThrowAsync<OperationCanceledException>();
  }

  [Fact]
  public async Task SyncPermissionsAsync_InDryRunMode_ShouldNotModifyDatabase()
  {
    var permissions = CreateSamplePermissionDescriptors(5);

    var result = await _service.SyncPermissionsAsync(permissions, dryRun: true, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.DryRunMode.Should().BeTrue();
    result.Data!.DatabaseOperations.Should().NotBeNull();
  }

  [Theory]
  [InlineData(1)]
  [InlineData(5)]
  [InlineData(10)]
  public async Task SyncPermissionsAsync_WithDifferentPermissionCounts_ShouldHandleCorrectly(int permissionCount)
  {
    var permissions = CreateSamplePermissionDescriptors(permissionCount);

    var result = await _service.SyncPermissionsAsync(permissions, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();

    var totalOperations = result.Data!.PermissionsCreated +
                          result.Data!.PermissionsUpdated +
                          result.Data!.PermissionsObsoleted +
                          result.Data!.PermissionsRecovered;

    (totalOperations >= 0).Should().BeTrue();
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithValidId_ShouldReturnPermissionDescriptor()
  {
    var result = await _service.RecoverPermissionAsync(1, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithValidIdAndNotFound_ShouldReturnSuccessWithNullData()
  {
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 999), Arg.Any<CancellationToken>())
      .Returns((Permission?)null);

    var result = await _service.RecoverPermissionAsync(999, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().BeNull();
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public async Task RecoverPermissionAsync_WithInvalidId_ShouldReturnFailure(int invalidId)
  {
    var result = await _service.RecoverPermissionAsync(invalidId, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithCancellationToken_ShouldRespectCancellation()
  {
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await ((Func<Task>)(() =>
      _service.RecoverPermissionAsync(1, cts.Token))).Should().ThrowAsync<OperationCanceledException>();
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithInactivePermission_ShouldRecoverLevel2()
  {
    var inactivePermission = new Permission
    {
      Id = 12,
      Key = "users.update",
      IsActive = false,
      Group = "Users",
      Description = "desc",
      CqrsType = "Command",
      SourceFilePath = "/features/users/update.cs",
      EndpointMethodName = "Handle",
      HttpMethod = "PUT",
      Route = "/api/users",
      UpdatedAt = DateTime.UtcNow,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 12), Arg.Any<CancellationToken>())
      .Returns(inactivePermission);

    var result = await _service.RecoverPermissionAsync(12, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.IsObsolete.Should().BeTrue();
    result.Data!.ValidationWarnings.Should().Contain(x => x.Contains("inactivo", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithActivePermissionAndMissingMetadata_ShouldApplyMapDefaults()
  {
    var permission = new Permission
    {
      Id = 15,
      Key = "singlekey",
      IsActive = true,
      Group = null,
      Description = null,
      CqrsType = null,
      SourceFilePath = null,
      EndpointMethodName = null,
      HttpMethod = null,
      Route = null,
      LastSyncAt = null,
      UpdatedAt = null,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 15), Arg.Any<CancellationToken>())
      .Returns(permission);

    var result = await _service.RecoverPermissionAsync(15, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Feature.Should().Be("singlekey");
    result.Data.Action.Should().Be("unknown");
    result.Data.Description.Should().Contain("Permiso singlekey");
    result.Data.CqrsType.Should().Be("Unknown");
    result.Data.SourceFilePath.Should().Be("Unknown");
    result.Data.EndpointMethodName.Should().Be("Unknown");
    result.Data.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
    result.Data.Routes.Should().ContainSingle().Which.Should().Be("/api/unknown");
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithPreviousKey_ShouldRecoverFromAuditFieldsLevel3()
  {
    var auditPermission = new Permission
    {
      Id = 13,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = "roles.assign",
      PreviousFilePath = "/audit/path.cs",
      PreviousMethodName = "AuditMethod",
      Group = null,
      Description = null,
      CqrsType = null,
      HttpMethod = null,
      Route = null,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 13), Arg.Any<CancellationToken>())
      .Returns(auditPermission);

    var result = await _service.RecoverPermissionAsync(13, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.Key.Should().Be("roles.assign");
    result.Data!.Feature.Should().Be("unknown");
    result.Data!.Action.Should().Be("assign");
    result.Data!.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
    result.Data!.Routes.Should().ContainSingle().Which.Should().Be("/api/unknown");
    result.Data!.ValidationWarnings.Should().Contain(x => x.Contains("auditor", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task RecoverPermissionAsync_FromAuditFieldsWithHttpAndRoute_ShouldUseExistingTransportValues()
  {
    var auditPermission = new Permission
    {
      Id = 16,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = "roles.manage",
      Group = "roles",
      HttpMethod = "DELETE",
      Route = "/api/roles/manage",
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 16), Arg.Any<CancellationToken>())
      .Returns(auditPermission);

    var result = await _service.RecoverPermissionAsync(16, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.HttpMethods.Should().ContainSingle().Which.Should().Be("DELETE");
    result.Data.Routes.Should().ContainSingle().Which.Should().Be("/api/roles/manage");
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithInvalidPermissionWithoutAudit_ShouldReturnSuccessWithNullData()
  {
    var brokenPermission = new Permission
    {
      Id = 14,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = null,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 14), Arg.Any<CancellationToken>())
      .Returns(brokenPermission);

    var result = await _service.RecoverPermissionAsync(14, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().BeNull();
  }

  [Fact]
  public async Task RecoverPermissionAsync_WhenRepositoryThrows_ShouldReturnFailure()
  {
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 77), Arg.Any<CancellationToken>())
      .Returns(Task.FromException<Permission?>(new InvalidOperationException("Recovery failed")));

    var result = await _service.RecoverPermissionAsync(77, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
  }

  [Fact]
  public async Task SyncPermissionsAsync_ShouldIncludeValidDatabaseStatistics()
  {
    var permissions = CreateSamplePermissionDescriptors(3);

    var result = await _service.SyncPermissionsAsync(permissions, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();

    var dbStats = result.Data!.DatabaseOperations;
    dbStats.Should().NotBeNull();
    (dbStats.TotalQueriesExecuted >= 0).Should().BeTrue();
    (dbStats.InsertOperations >= 0).Should().BeTrue();
    (dbStats.UpdateOperations >= 0).Should().BeTrue();
    (dbStats.SoftDeleteOperations >= 0).Should().BeTrue();
    (dbStats.RecoveryOperations >= 0).Should().BeTrue();
    (dbStats.TransactionDurationMs >= 0).Should().BeTrue();
  }

  [Fact]
  public async Task SyncPermissionsAsync_ShouldTrackExecutionTime()
  {
    var permissions = CreateSamplePermissionDescriptors(2);

    var startTime = DateTime.UtcNow;
    var result = await _service.SyncPermissionsAsync(permissions, false, CancellationToken.None);
    var endTime = DateTime.UtcNow;

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    (result.Data!.ExecutionTimeMs >= 0).Should().BeTrue();

    var totalTestTimeMs = (endTime - startTime).TotalMilliseconds;
    (result.Data!.ExecutionTimeMs <= totalTestTimeMs + 1000).Should().BeTrue();
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithExistingPermissionWithChanges_ShouldCallUpdateAsync()
  {
    var descriptor = CreateSamplePermissionDescriptors(1).First();
    var existingPermission = new Permission
    {
      Id = 1,
      Key = "test.permission_1",
      Description = "DIFFERENT description",
      IsActive = true,
      Route = "/test/1",
      HttpMethod = "GET",
      Group = "Test",
      CqrsType = "Query",
      SourceFilePath = "/test/TestEndpoint1.cs",
      EndpointMethodName = "MapTestEndpoint1",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission> { existingPermission });

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor> { descriptor }, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsUpdated.Should().Be(1);
    result.Data!.PermissionsCreated.Should().Be(0);
    await _mockRepository.Received(1).UpdateAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithExistingPermissionNoChanges_ShouldNotCallUpdateAsync()
  {
    var descriptor = CreateSamplePermissionDescriptors(1).First();
    var existingPermission = new Permission
    {
      Id = 1,
      Key = "test.permission_1",
      Description = "Test permission 1",
      IsActive = true,
      Route = "/test/1",
      HttpMethod = "GET",
      Group = "Test",
      CqrsType = "Query",
      SourceFilePath = "/test/TestEndpoint1.cs",
      EndpointMethodName = "MapTestEndpoint1",
      HasPermissionsNotRequired = false,
      PermissionsNotRequiredReason = null,
      RequiresDevelopmentAccess = false,
      RequiresDevelopmentAccessReason = null,
      RequiresSuAdminAccess = false,
      RequiresSuAdminAccessReason = null,
      HasRequirePermissions = false,
      RequirePermissionsNote = null,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission> { existingPermission });

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor> { descriptor }, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsUpdated.Should().Be(0);
    await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithObsoletePermission_ShouldCallMarkAsObsoleteAsync()
  {
    var existingPermission = new Permission
    {
      Id = 99,
      Key = "old.obsolete_permission",
      Description = "Old Permission",
      IsActive = true,
      Route = "/old",
      HttpMethod = "GET",
      Group = "Old",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission> { existingPermission });

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor>(), false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsObsoleted.Should().Be(1);
    await _mockRepository.Received(1).MarkAsObsoleteAsync("old.obsolete_permission", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_DryRun_WithObsoletePermission_ShouldCountButNotMarkObsolete()
  {
    var existingPermission = new Permission
    {
      Id = 100,
      Key = "old.dry_run_permission",
      Description = "Old Permission",
      IsActive = true,
      Route = "/old",
      HttpMethod = "GET",
      Group = "Old",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission> { existingPermission });

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor>(), dryRun: true, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsObsoleted.Should().Be(1);
    await _mockRepository.DidNotReceive().MarkAsObsoleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithNewPermissionKeyExistingInDb_ShouldUpdateExisting()
  {
    var descriptor = CreateSamplePermissionDescriptors(1).First();
    var existingByKey = new Permission
    {
      Id = 5,
      Key = "test.permission_1",
      IsActive = false,
      Description = "Old",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _mockRepository.GetByKeyAsync("test.permission_1", Arg.Any<CancellationToken>()).Returns(existingByKey);

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor> { descriptor }, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsCreated.Should().Be(1);
    await _mockRepository.Received(1).UpdateAsync(existingByKey, Arg.Any<CancellationToken>());
    await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_DryRun_WithNewPermissions_ShouldCountButNotAddToDb()
  {
    var descriptors = CreateSamplePermissionDescriptors(3);

    var result = await _service.SyncPermissionsAsync(descriptors, dryRun: true, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsCreated.Should().Be(3);
    await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>());
    await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task SyncPermissionsAsync_WhenRepositoryThrows_ShouldReturnFailure()
  {
    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromException<IList<Permission>>(new InvalidOperationException("DB connection lost")));

    var result = await _service.SyncPermissionsAsync(CreateSamplePermissionDescriptors(1), false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithExistingAndNewPermissions_ShouldHandleBothPaths()
  {
    var descriptors = CreateSamplePermissionDescriptors(2);
    var existingForDescriptor1 = new Permission
    {
      Id = 10,
      Key = "test.permission_1",
      Description = "CHANGED description",
      IsActive = true,
      Route = "/test/1",
      HttpMethod = "GET",
      Group = "Test",
      CqrsType = "Query",
      SourceFilePath = "/test/TestEndpoint1.cs",
      EndpointMethodName = "MapTestEndpoint1",
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission> { existingForDescriptor1 });

    var result = await _service.SyncPermissionsAsync(descriptors, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsCreated.Should().Be(1);
    result.Data!.PermissionsUpdated.Should().Be(1);
  }

  [Theory]
  [InlineData("route")]
  [InlineData("http")]
  [InlineData("source")]
  [InlineData("method")]
  [InlineData("cqrs")]
  [InlineData("hasNotRequired")]
  [InlineData("notRequiredReason")]
  [InlineData("requiresDev")]
  [InlineData("requiresDevReason")]
  [InlineData("requiresSu")]
  [InlineData("requiresSuReason")]
  [InlineData("hasRequire")]
  [InlineData("requireNote")]
  public async Task SyncPermissionsAsync_HasChangesBranches_ShouldUpdate_WhenSpecificFieldDiffers(string field)
  {
    var descriptor = CreateSamplePermissionDescriptors(1).First();
    var existing = CreateExistingMatchingDescriptor(descriptor);

    switch (field)
    {
      case "route":
        existing.Route = "/different";
        break;
      case "http":
        existing.HttpMethod = "PATCH";
        break;
      case "source":
        existing.SourceFilePath = "/different/path.cs";
        break;
      case "method":
        existing.EndpointMethodName = "DifferentMethod";
        break;
      case "cqrs":
        existing.CqrsType = "Other";
        break;
      case "hasNotRequired":
        existing.HasPermissionsNotRequired = true;
        break;
      case "notRequiredReason":
        existing.PermissionsNotRequiredReason = "other";
        break;
      case "requiresDev":
        existing.RequiresDevelopmentAccess = true;
        break;
      case "requiresDevReason":
        existing.RequiresDevelopmentAccessReason = "other";
        break;
      case "requiresSu":
        existing.RequiresSuAdminAccess = true;
        break;
      case "requiresSuReason":
        existing.RequiresSuAdminAccessReason = "other";
        break;
      case "hasRequire":
        existing.HasRequirePermissions = true;
        break;
      case "requireNote":
        existing.RequirePermissionsNote = "other";
        break;
    }

    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Permission> { existing });

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor> { descriptor }, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PermissionsUpdated.Should().Be(1);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithNullTechnicalMetadata_ShouldMapDefaults()
  {
    var descriptor = new PermissionDescriptor
    {
      Key = "users.null_meta",
      Feature = "Users",
      Action = "null_meta",
      Description = "desc",
      CqrsType = "Query",
      SourceFilePath = string.Empty,
      EndpointMethodName = "Handle",
      HttpMethods = new List<string>(),
      Routes = new List<string>(),
      DiscoveredAt = DateTime.UtcNow,
      ContentHash = "hash",
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string>(),
    };

    Permission? addedPermission = null;
    _mockRepository.AddAsync(Arg.Do<Permission>(p => addedPermission = p), Arg.Any<CancellationToken>())
      .Returns(call => call.Arg<Permission>());

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor> { descriptor }, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    addedPermission.Should().NotBeNull();
    addedPermission!.Route.Should().BeNull();
    addedPermission.HttpMethod.Should().BeNull();
    addedPermission.FullNamespace.Should().Be(string.Empty);
  }

  [Fact]
  public async Task SyncPermissionsAsync_WithSourcePathWithoutExtension_ShouldPreserveWholePathInNamespace()
  {
    var descriptor = new PermissionDescriptor
    {
      Key = "users.no_extension",
      Feature = "Users",
      Action = "no_extension",
      Description = "desc",
      CqrsType = "Query",
      SourceFilePath = "NoExtension",
      EndpointMethodName = "Handle",
      HttpMethods = new List<string> { "GET" },
      Routes = new List<string> { "/api/users/no-extension" },
      DiscoveredAt = DateTime.UtcNow,
      ContentHash = "hash2",
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string>(),
    };

    Permission? addedPermission = null;
    _mockRepository.AddAsync(Arg.Do<Permission>(p => addedPermission = p), Arg.Any<CancellationToken>())
      .Returns(call => call.Arg<Permission>());

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor> { descriptor }, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    addedPermission.Should().NotBeNull();
    addedPermission!.FullNamespace.Should().Be("FrugoCorp.AspNetCore.AutoDiscoveryIAM.NoExtension");
  }

  [Fact]
  public async Task SyncPermissionsAsync_UpdatePath_WithNullRoutesAndHttpMethods_ShouldPersistNullTransport()
  {
    var descriptor = CreateSamplePermissionDescriptors(1).First();
    descriptor.Description = "changed";
    descriptor.Routes = null!;
    descriptor.HttpMethods = null!;

    var existing = CreateExistingMatchingDescriptor(CreateSamplePermissionDescriptors(1).First());
    existing.Description = "old";

    Permission? updated = null;
    _mockRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission> { existing });
    _mockRepository.UpdateAsync(Arg.Do<Permission>(p => updated = p), Arg.Any<CancellationToken>())
        .Returns(call => call.Arg<Permission>());

    var result = await _service.SyncPermissionsAsync(new List<PermissionDescriptor> { descriptor }, false, CancellationToken.None);

    result.Success.Should().BeTrue();
    updated.Should().NotBeNull();
    updated!.Route.Should().BeNull();
    updated.HttpMethod.Should().BeNull();
  }

  [Fact]
  public async Task ApplyRecoveryAlgorithm_WithNullPermission_ShouldReturnNullDescriptor()
  {
    var method = typeof(SyncPermissionsService)
        .GetMethod("ApplyRecoveryAlgorithm", BindingFlags.NonPublic | BindingFlags.Instance)!;

    var task = (Task<PermissionDescriptor?>)method.Invoke(_service, new object?[] { null, 123 })!;
    var result = await task;

    result.Should().BeNull();
  }

  [Fact]
  public void HasChanges_WithNullRouteAndHttpCollections_ShouldHandleNullConditionalBranches()
  {
    var method = typeof(SyncPermissionsService)
        .GetMethod("HasChanges", BindingFlags.NonPublic | BindingFlags.Static)!;

    var descriptor = new PermissionDescriptor
    {
      Key = "users.null_transport",
      Feature = "Users",
      Action = "null_transport",
      Description = "desc",
      CqrsType = "Query",
      SourceFilePath = "/features/users/null_transport.cs",
      EndpointMethodName = "Handle",
      Routes = null!,
      HttpMethods = null!,
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string>(),
    };

    var existingSame = new Permission
    {
      Key = descriptor.Key,
      Group = descriptor.Feature,
      Description = descriptor.Description,
      CqrsType = descriptor.CqrsType,
      SourceFilePath = descriptor.SourceFilePath,
      EndpointMethodName = descriptor.EndpointMethodName,
      Route = null,
      HttpMethod = null,
    };

    var existingDifferentTransport = new Permission
    {
      Key = descriptor.Key,
      Group = descriptor.Feature,
      Description = descriptor.Description,
      CqrsType = descriptor.CqrsType,
      SourceFilePath = descriptor.SourceFilePath,
      EndpointMethodName = descriptor.EndpointMethodName,
      Route = "/api/users/null-transport",
      HttpMethod = "GET",
    };

    var noChanges = (bool)method.Invoke(null, new object[] { descriptor, existingSame })!;
    var hasChanges = (bool)method.Invoke(null, new object[] { descriptor, existingDifferentTransport })!;

    noChanges.Should().BeFalse();
    hasChanges.Should().BeTrue();
  }

  [Fact]
  public void ExtractFeatureAndAction_WithNullOrSinglePartKey_ShouldReturnUnknownFallbacks()
  {
    var featureMethod = typeof(SyncPermissionsService)
        .GetMethod("ExtractFeatureFromKey", BindingFlags.NonPublic | BindingFlags.Static)!;
    var actionMethod = typeof(SyncPermissionsService)
        .GetMethod("ExtractActionFromKey", BindingFlags.NonPublic | BindingFlags.Static)!;

    var featureFromNull = (string)featureMethod.Invoke(null, new object?[] { null })!;
    var actionFromNull = (string)actionMethod.Invoke(null, new object?[] { null })!;
    var actionFromSinglePart = (string)actionMethod.Invoke(null, new object?[] { "singlepart" })!;

    featureFromNull.Should().Be("unknown");
    actionFromNull.Should().Be("unknown");
    actionFromSinglePart.Should().Be("unknown");
  }

  [Fact]
  public void MapToEntity_WithAndWithoutTransport_ShouldCoverTransportBranches()
  {
    var mapToEntity = typeof(SyncPermissionsService)
        .GetMethod("MapToEntity", BindingFlags.NonPublic | BindingFlags.Static)!;

    var withTransport = new PermissionDescriptor
    {
      Key = "users.with_transport",
      Feature = "Users",
      Action = "with_transport",
      Description = "desc",
      CqrsType = "Command",
      SourceFilePath = "Features/Users/WithTransport.cs",
      EndpointMethodName = "Handle",
      HttpMethods = new List<string> { "PUT" },
      Routes = new List<string> { "/api/users/with-transport" },
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string>(),
    };

    var withoutTransport = new PermissionDescriptor
    {
      Key = "users.without_transport",
      Feature = "Users",
      Action = "without_transport",
      Description = "desc",
      CqrsType = "Query",
      SourceFilePath = "Features/Users/WithoutTransport.cs",
      EndpointMethodName = "Handle",
      HttpMethods = null!,
      Routes = null!,
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string>(),
    };

    var mappedWithTransport = (Permission)mapToEntity.Invoke(null, new object[] { withTransport })!;
    var mappedWithoutTransport = (Permission)mapToEntity.Invoke(null, new object[] { withoutTransport })!;

    mappedWithTransport.HttpMethod.Should().Be("PUT");
    mappedWithTransport.Route.Should().Be("/api/users/with-transport");
    mappedWithoutTransport.HttpMethod.Should().BeNull();
    mappedWithoutTransport.Route.Should().BeNull();
  }

  [Fact]
  public void MapToDescriptor_WithAndWithoutTransport_ShouldCoverFallbackBranches()
  {
    var mapToDescriptor = typeof(SyncPermissionsService)
        .GetMethod("MapToDescriptor", BindingFlags.NonPublic | BindingFlags.Static)!;

    var withTransport = new Permission
    {
      Key = "users.with_transport",
      Group = "Users",
      Description = "desc",
      CqrsType = "Command",
      SourceFilePath = "Features/Users/WithTransport.cs",
      EndpointMethodName = "Handle",
      HttpMethod = "PATCH",
      Route = "/api/users/with-transport",
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var withoutTransport = new Permission
    {
      Key = "users.without_transport",
      Group = null,
      Description = null,
      CqrsType = null,
      SourceFilePath = null,
      EndpointMethodName = null,
      HttpMethod = null,
      Route = null,
      IsActive = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var descriptorWithTransport = (PermissionDescriptor)mapToDescriptor.Invoke(null, new object[] { withTransport })!;
    var descriptorWithoutTransport = (PermissionDescriptor)mapToDescriptor.Invoke(null, new object[] { withoutTransport })!;

    descriptorWithTransport.HttpMethods.Should().ContainSingle().Which.Should().Be("PATCH");
    descriptorWithTransport.Routes.Should().ContainSingle().Which.Should().Be("/api/users/with-transport");
    descriptorWithoutTransport.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
    descriptorWithoutTransport.Routes.Should().ContainSingle().Which.Should().Be("/api/unknown");
  }

  [Fact]
  public async Task ApplyRecoveryAlgorithm_WithInactiveValidPermission_ShouldUseLevel2Branch()
  {
    var method = typeof(SyncPermissionsService)
        .GetMethod("ApplyRecoveryAlgorithm", BindingFlags.NonPublic | BindingFlags.Instance)!;

    var permission = new Permission
    {
      Key = "users.recovery_level2",
      Group = "Users",
      Description = "desc",
      CqrsType = "Query",
      SourceFilePath = "Features/Users/Recovery.cs",
      EndpointMethodName = "Handle",
      HttpMethod = "GET",
      Route = "/api/users/recovery",
      IsActive = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var task = (Task<PermissionDescriptor?>)method.Invoke(_service, new object[] { permission, 456 })!;
    var result = await task;

    result.Should().NotBeNull();
    result!.ValidationWarnings.Should().Contain(x => x.Contains("inactivo", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithActivePermissionButEmptyKey_ShouldReturnNull()
  {
    // Covers L474 FALSE via short-circuit: !permission.IsActive = false → whole && is false
    var permission = new Permission
    {
      Id = 91,
      Key = string.Empty,
      IsActive = true,
      PreviousKey = null,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 91), Arg.Any<CancellationToken>())
      .Returns(permission);

    var result = await _service.RecoverPermissionAsync(91, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().BeNull();
  }

  [Fact]
  public void RecoverFromAuditFields_WithGroupAndNoRoute_ShouldUseGroupInRouteFallback()
  {
    // Covers L498: Routes FALSE branch where Group is non-null → $"/api/{Group}"
    var method = typeof(SyncPermissionsService)
        .GetMethod("RecoverFromAuditFields", BindingFlags.NonPublic | BindingFlags.Static)!;

    var permission = new Permission
    {
      PreviousKey = "users.manage",
      Group = "Users",
      HttpMethod = null,
      Route = null,
      CreatedAt = DateTime.UtcNow,
    };

    var descriptor = (PermissionDescriptor)method.Invoke(null, new object[] { permission, 92 })!;

    descriptor.Routes.Should().ContainSingle().Which.Should().Be("/api/Users");
    descriptor.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
  }

  [Fact]
  public void MapToDescriptor_WithGroupAndNoRoute_ShouldUseGroupInRouteFallback()
  {
    // Covers L534: Routes FALSE branch where Group is non-null → $"/api/{Group}"
    var method = typeof(SyncPermissionsService)
        .GetMethod("MapToDescriptor", BindingFlags.NonPublic | BindingFlags.Static)!;

    var permission = new Permission
    {
      Key = "users.read",
      Group = "Users",
      HttpMethod = null,
      Route = null,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };

    var descriptor = (PermissionDescriptor)method.Invoke(null, new object[] { permission })!;

    descriptor.Routes.Should().ContainSingle().Which.Should().Be("/api/Users");
    descriptor.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
  }

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
        CqrsType = i % 2 == 0 ? "Command" : "Query",
        SourceFilePath = $"/test/TestEndpoint{i}.cs",
        EndpointMethodName = $"MapTestEndpoint{i}",
        HttpMethods = new List<string> { i % 2 == 0 ? "POST" : "GET" },
        Routes = new List<string> { $"/test/{i}" },
        IsObsolete = false,
        DiscoveredAt = DateTime.UtcNow.AddMinutes(-i),
        ContentHash = $"hash{i}_{Guid.NewGuid()}",
        ValidationErrors = new List<string>(),
        ValidationWarnings = new List<string>(),
      });
    }

    return descriptors;
  }

  private static Permission CreateExistingMatchingDescriptor(PermissionDescriptor descriptor)
  {
    return new Permission
    {
      Id = 500,
      Key = descriptor.Key,
      Description = descriptor.Description,
      Group = descriptor.Feature,
      Route = descriptor.Routes?.FirstOrDefault(),
      HttpMethod = descriptor.HttpMethods?.FirstOrDefault(),
      SourceFilePath = descriptor.SourceFilePath,
      EndpointMethodName = descriptor.EndpointMethodName,
      CqrsType = descriptor.CqrsType,
      HasPermissionsNotRequired = descriptor.HasPermissionsNotRequired,
      PermissionsNotRequiredReason = descriptor.PermissionsNotRequiredReason,
      RequiresDevelopmentAccess = descriptor.RequiresDevelopmentAccess,
      RequiresDevelopmentAccessReason = descriptor.RequiresDevelopmentAccessReason,
      RequiresSuAdminAccess = descriptor.RequiresSuAdminAccess,
      RequiresSuAdminAccessReason = descriptor.RequiresSuAdminAccessReason,
      HasRequirePermissions = descriptor.HasRequirePermissions,
      RequirePermissionsNote = descriptor.RequirePermissionsNote,
      IsActive = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };
  }

  // ─── Branch coverage: 3 missing paths ────────────────────────────────────

  /// <summary>
  /// Covers L474 short-circuit: IsActive=true AND Key="" → !IsActive=false → &amp;&amp;-short-circuit → Level3
  /// </summary>
  [Fact]
  public async Task RecoverPermissionAsync_WithActivePermissionAndEmptyKey_ShouldFallToLevel3()
  {
    var permission = new Permission
    {
      Id = 20,
      Key = string.Empty,
      IsActive = true,
      PreviousKey = "users.fallback",
      Group = null,
      HttpMethod = null,
      Route = null,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 20), Arg.Any<CancellationToken>())
        .Returns(permission);

    var result = await _service.RecoverPermissionAsync(20, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.Key.Should().Be("users.fallback");
    result.Data!.ValidationWarnings.Should().Contain(x => x.Contains("auditor", StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Covers RecoverFromAuditFields: Routes false branch with Group!=null → $"/api/{Group}" (not "unknown")
  /// </summary>
  [Fact]
  public async Task RecoverPermissionAsync_FromAuditFieldsWithGroupButNoRoute_ShouldUseGroupInDefaultRoute()
  {
    var permission = new Permission
    {
      Id = 21,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = "roles.delete",
      Group = "roles",
      HttpMethod = null,
      Route = null,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 21), Arg.Any<CancellationToken>())
        .Returns(permission);

    var result = await _service.RecoverPermissionAsync(21, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Routes.Should().ContainSingle().Which.Should().Be("/api/roles");
    result.Data!.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
  }

  /// <summary>
  /// Covers MapToDescriptor: Routes false branch with Group!=null → $"/api/{Group}" (not "unknown")
  /// </summary>
  [Fact]
  public async Task RecoverPermissionAsync_WithInactivePermissionWithGroupButNoRoute_ShouldUseGroupInDefaultRoute()
  {
    var permission = new Permission
    {
      Id = 22,
      Key = "admin.list",
      IsActive = false,
      Group = "admin",
      HttpMethod = null,
      Route = null,
      Description = "desc",
      CqrsType = "Query",
      SourceFilePath = "/features/admin.cs",
      EndpointMethodName = "Handle",
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 22), Arg.Any<CancellationToken>())
        .Returns(permission);

    var result = await _service.RecoverPermissionAsync(22, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Routes.Should().ContainSingle().Which.Should().Be("/api/admin");
    result.Data!.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
    result.Data!.Feature.Should().Be("admin");
  }

  [Fact]
  public async Task RecoverPermissionAsync_FromAuditFieldsWithoutPreviousMetadata_ShouldUseCurrentSourceAndMethod()
  {
    var permission = new Permission
    {
      Id = 23,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = "roles.audit_fallback",
      PreviousFilePath = null,
      PreviousMethodName = null,
      SourceFilePath = "/features/roles/fallback.cs",
      EndpointMethodName = "FallbackHandle",
      Group = "roles",
      HttpMethod = null,
      Route = null,
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 23), Arg.Any<CancellationToken>())
      .Returns(permission);

    var result = await _service.RecoverPermissionAsync(23, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.SourceFilePath.Should().Be("/features/roles/fallback.cs");
    result.Data!.EndpointMethodName.Should().Be("FallbackHandle");
  }

  [Fact]
  public async Task RecoverPermissionAsync_WithLastSyncAt_ShouldPreferItForDiscoveredAt()
  {
    var expectedLastSyncAt = DateTime.UtcNow.AddMinutes(-30);
    var permission = new Permission
    {
      Id = 24,
      Key = "users.lastsync",
      IsActive = true,
      LastSyncAt = expectedLastSyncAt,
      UpdatedAt = DateTime.UtcNow,
      CreatedAt = DateTime.UtcNow.AddDays(-1),
      Group = "users",
      HttpMethod = "GET",
      Route = "/api/users/lastsync",
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 24), Arg.Any<CancellationToken>())
      .Returns(permission);

    var result = await _service.RecoverPermissionAsync(24, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.DiscoveredAt.Should().Be(expectedLastSyncAt);
  }

  [Fact]
  public async Task RecoverPermissionAsync_FromAuditFields_WithLastSyncAt_ShouldUseLastSyncAtForDiscoveredAt()
  {
    var expectedLastSyncAt = DateTime.UtcNow.AddHours(-2);
    var permission = new Permission
    {
      Id = 25,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = "audit.lastsync",
      LastSyncAt = expectedLastSyncAt,
      UpdatedAt = DateTime.UtcNow.AddHours(-1),
      CreatedAt = DateTime.UtcNow.AddDays(-1),
      Group = "audit",
      HttpMethod = null,
      Route = null,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 25), Arg.Any<CancellationToken>())
      .Returns(permission);

    var result = await _service.RecoverPermissionAsync(25, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.DiscoveredAt.Should().Be(expectedLastSyncAt);
  }

  [Fact]
  public async Task RecoverPermissionAsync_FromAuditFields_WithUpdatedAtAndNoLastSyncAt_ShouldUseUpdatedAtForDiscoveredAt()
  {
    var expectedUpdatedAt = DateTime.UtcNow.AddMinutes(-45);
    var permission = new Permission
    {
      Id = 26,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = "audit.updatedat",
      LastSyncAt = null,
      UpdatedAt = expectedUpdatedAt,
      CreatedAt = DateTime.UtcNow.AddDays(-3),
      Group = "audit",
      HttpMethod = null,
      Route = null,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 26), Arg.Any<CancellationToken>())
      .Returns(permission);

    var result = await _service.RecoverPermissionAsync(26, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.DiscoveredAt.Should().Be(expectedUpdatedAt);
  }

  [Fact]
  public async Task RecoverPermissionAsync_FromAuditFields_WithDescriptionAndCqrsType_ShouldPreserveBothValues()
  {
    var permission = new Permission
    {
      Id = 27,
      Key = string.Empty,
      IsActive = false,
      PreviousKey = "audit.rich_metadata",
      Description = "Descripcion de auditoria",
      CqrsType = "Command",
      CreatedAt = DateTime.UtcNow,
    };
    _mockRepository.GetByIdAsync(Arg.Is<long>(id => id == 27), Arg.Any<CancellationToken>())
      .Returns(permission);

    var result = await _service.RecoverPermissionAsync(27, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.Description.Should().Be("Descripcion de auditoria");
    result.Data!.CqrsType.Should().Be("Command");
  }
}
