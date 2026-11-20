// -----------------------------------------------------------------------
// <copyright file="DiscoveryPermissionsServiceTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using DiscoveryPermissionsServiceImpl = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService.DiscoveryPermissionsService;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions.Services;

/// <summary>
/// Tests unitarios para DiscoveryPermissionsService.
/// </summary>
/// <remarks>
/// El único mecanismo de descubrimiento es <see cref="IEndpointDiscoveryService"/> (vía
/// EndpointDataSource) -- no existe fallback por reflection: si el descubrimiento falla o no
/// encuentra nada, la operación reporta un error, en vez de ocultarlo intentando otro camino.
/// </remarks>
public class DiscoveryPermissionsServiceTests
{
  private readonly IPermissionLogger _mockLogger;
  private readonly IEndpointDiscoveryService _mockEndpointDiscoveryService;
  private readonly ISyncPermissionsRepository _mockPermissionRepository;
  private readonly DiscoveryPermissionsServiceImpl _service;

  public DiscoveryPermissionsServiceTests()
  {
    _mockLogger = Substitute.For<IPermissionLogger>();
    _mockEndpointDiscoveryService = Substitute.For<IEndpointDiscoveryService>();
    _mockPermissionRepository = Substitute.For<ISyncPermissionsRepository>();

    _mockPermissionRepository.GetAllAsync(Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<IList<Permission>>(new List<Permission>()));

    _service = new DiscoveryPermissionsServiceImpl(_mockLogger, _mockEndpointDiscoveryService, _mockPermissionRepository);
  }

  [Fact]
  public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
  {
    var act = () => _ = new DiscoveryPermissionsServiceImpl(null!, _mockEndpointDiscoveryService, _mockPermissionRepository);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WithNullEndpointDiscoveryService_ShouldThrowArgumentNullException()
  {
    var act = () => _ = new DiscoveryPermissionsServiceImpl(_mockLogger, null!, _mockPermissionRepository);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
  {
    var act = () => _ = new DiscoveryPermissionsServiceImpl(_mockLogger, _mockEndpointDiscoveryService, null!);

    act.Should().Throw<ArgumentNullException>();
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task DiscoverPermissionsAsync_WhenMetadataReturnsPermissions_ShouldReturnThem(bool forceFullScan)
  {
    var metadataPermissions = new List<PermissionDescriptor>
    {
      CreatePermissionDescriptor("meta.permission", "Meta", "permission"),
    };

    _mockEndpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(metadataPermissions);

    var result = await _service.DiscoverPermissionsAsync(forceFullScan, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().HaveCount(1);
    result.Data![0].Key.Should().Be("meta.permission");
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WhenMetadataReturnsEmpty_ShouldReturnNotFound()
  {
    _mockEndpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(new List<PermissionDescriptor>());

    var result = await _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WhenMetadataThrows_ShouldReturnServerErrorWithoutFallback()
  {
    _mockEndpointDiscoveryService
      .DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromException<IList<PermissionDescriptor>>(new InvalidOperationException("metadata failed")));

    var result = await _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    _mockLogger.Received().LogDiscoveryError(Arg.Is<string>(x => x.Contains("metadata failed")));
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WhenLoadingExistingPermissionsFails_ShouldStillSucceed()
  {
    var metadataPermissions = new List<PermissionDescriptor>
    {
      CreatePermissionDescriptor("meta.permission", "Meta", "permission"),
    };
    _mockEndpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(metadataPermissions);
    _mockPermissionRepository.GetAllAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromException<IList<Permission>>(new InvalidOperationException("db failed")));

    var result = await _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().HaveCount(1);
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WhenUnexpectedExceptionOccurs_ShouldReturnServerError()
  {
    _mockLogger.When(x => x.LogDiscoveryStarted(Arg.Any<bool>()))
      .Do(_ => throw new InvalidOperationException("logger exploded"));

    var result = await _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    (result.Error ?? string.Empty).ToLowerInvariant().Should().Contain("discovery");
    _mockLogger.Received().LogDiscoveryError(Arg.Is<string>(x => x.Contains("logger exploded")));
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WhenCompletionLoggingThrows_ShouldReturnServerError()
  {
    _mockEndpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(new List<PermissionDescriptor> { CreatePermissionDescriptor("meta.permission", "Meta", "permission") });
    _mockLogger.WhenForAnyArgs(x => x.LogDiscoveryCompleted(default, default))
      .Do(_ => throw new InvalidOperationException("completed exploded"));

    var result = await _service.DiscoverPermissionsAsync(false, false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    _mockLogger.Received().LogDiscoveryError(Arg.Is<string>(x => x.Contains("completed exploded")));
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WithDetailedConsoleOutput_ShouldPrintNewModifiedAndUnchangedStatuses()
  {
    var permissions = new List<PermissionDescriptor>
    {
      CreatePermissionDescriptor("users.new_one", "Users", "new_one"),
      CreatePermissionDescriptor("users.same_one", "Users", "same_one"),
      CreatePermissionDescriptor("users.changed_one", "Users", "changed_one"),
    };
    permissions[0].HasPermissionsNotRequired = true;
    permissions[0].PermissionsNotRequiredReason = "public";

    _mockEndpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(permissions);
    _mockPermissionRepository.GetAllAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Permission>
      {
        CreateExistingPermission(permissions[1]),
        CreateExistingPermission(permissions[2], description: "different description"),
      });

    using var writer = new StringWriter();
    var originalOut = Console.Out;
    Console.SetOut(writer);
    try
    {
      var result = await _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: true, CancellationToken.None);

      result.Success.Should().BeTrue();
      writer.ToString().Should().Contain("Nuevo");
      writer.ToString().Should().Contain("Sin cambios");
      writer.ToString().Should().Contain("Modificado");
    }
    finally
    {
      Console.SetOut(originalOut);
    }
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WithDetailedConsoleOutput_ShouldDetectLateAuthorizationFieldChange()
  {
    var permission = CreatePermissionDescriptor("users.changed_auth_note", "Users", "changed_auth_note");
    permission.HasRequirePermissions = true;
    permission.RequirePermissionsNote = "expected";

    _mockEndpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(new List<PermissionDescriptor> { permission });
    _mockPermissionRepository.GetAllAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Permission>
      {
        CreateExistingPermissionWithChangedRequireNote(permission),
      });

    using var writer = new StringWriter();
    var originalOut = Console.Out;
    Console.SetOut(writer);
    try
    {
      var result = await _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: true, CancellationToken.None);

      result.Success.Should().BeTrue();
      writer.ToString().Should().Contain("Modificado");
    }
    finally
    {
      Console.SetOut(originalOut);
    }
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WithDetailedConsoleOutput_AndRepositoryFailure_ShouldStillPrintNewStatus()
  {
    var permission = CreatePermissionDescriptor("users.repo_fail", "Users", "repo_fail");

    _mockEndpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(Arg.Any<CancellationToken>())
      .Returns(new List<PermissionDescriptor> { permission });
    _mockPermissionRepository.GetAllAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromException<IList<Permission>>(new InvalidOperationException("repo exploded")));

    using var writer = new StringWriter();
    var originalOut = Console.Out;
    Console.SetOut(writer);
    try
    {
      var result = await _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: true, CancellationToken.None);

      result.Success.Should().BeTrue();
      writer.ToString().Should().Contain("Nuevo");
    }
    finally
    {
      Console.SetOut(originalOut);
    }
  }

  [Fact]
  public void LogDiscoveredPermissions_WhenExistingPermissionsIsNull_ShouldPrintNewStatus()
  {
    var permissions = new List<PermissionDescriptor>
    {
      CreatePermissionDescriptor("users.null_existing", "Users", "null_existing"),
    };

    using var writer = new StringWriter();
    var originalOut = Console.Out;
    Console.SetOut(writer);
    try
    {
      typeof(DiscoveryPermissionsServiceImpl)
        .GetMethod("LogDiscoveredPermissions", BindingFlags.NonPublic | BindingFlags.Instance)!
        .Invoke(_service, new object?[] { permissions, null, true });

      writer.ToString().Should().Contain("Nuevo");
    }
    finally
    {
      Console.SetOut(originalOut);
    }
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WithCancellationToken_ShouldRespectCancellation()
  {
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await ((Func<Task>)(() =>
      _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: false, cts.Token))).Should().ThrowAsync<TaskCanceledException>();
  }

  [Fact]
  public async Task DiscoverPermissionsAsync_WhenOperationCanceledInsideTry_ShouldRethrowAsTaskCanceledException()
  {
    _mockLogger.When(x => x.LogDiscoveryStarted(Arg.Any<bool>()))
      .Do(_ => throw new OperationCanceledException("mid cancellation"));

    await ((Func<Task>)(() =>
      _service.DiscoverPermissionsAsync(false, enableDetailedConsoleOutput: false, CancellationToken.None))).Should().ThrowAsync<TaskCanceledException>();
  }

  [Fact]
  public void ValidateConsistency_WithValidDescriptors_ShouldReturnSuccess()
  {
    var descriptors = CreateValidPermissionDescriptors();

    var result = _service.ValidateConsistency(descriptors);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.Should().BeEmpty();
  }

  [Fact]
  public void ValidateConsistency_WithDuplicateKeys_ShouldReturnValidationErrors()
  {
    var descriptors = new List<PermissionDescriptor>
        {
            CreatePermissionDescriptor("users.create_user", "Users", "create_user"),
            CreatePermissionDescriptor("users.create_user", "Users", "create_user"),
        };

    var result = _service.ValidateConsistency(descriptors);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
    result.ErrorInfo!.Key.Should().Be("Status400Validation");
    result.Error.Should().NotBeNull();
  }

  [Fact]
  public void ValidateConsistency_WithInvalidKeys_ShouldReturnValidationErrors()
  {
    var descriptors = new List<PermissionDescriptor>
        {
            CreatePermissionDescriptor("", "Users", "create_user"),
            CreatePermissionDescriptor("users.valid", "", "create_user"),
            CreatePermissionDescriptor("users.another", "Users", ""),
        };

    var result = _service.ValidateConsistency(descriptors);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
    result.ErrorInfo!.Key.Should().Be("Status400Validation");
    result.Error.Should().NotBeNull();
  }

  [Fact]
  public void ValidateConsistency_WithMissingRequiredFields_ShouldReturnValidationErrors()
  {
    var descriptors = new List<PermissionDescriptor>
        {
            new PermissionDescriptor
            {
                Key = "users.create_user",
                Feature = "",
                Action = "create_user",
                CqrsType = "",
                SourceFilePath = "",
                EndpointMethodName = "",
                HttpMethods = new List<string>(),
                Routes = new List<string>(),
                ValidationErrors = new List<string>(),
                ValidationWarnings = new List<string>(),
            },
        };

    var result = _service.ValidateConsistency(descriptors);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().NotBeNull();
    result.ErrorInfo!.Key.Should().Be("Status400Validation");
    result.Error.Should().NotBeNull();
  }

  [Fact]
  public void ValidateConsistency_WithMissingRoutes_ShouldReturnValidationErrors()
  {
    var descriptor = CreatePermissionDescriptor("users.no_routes", "Users", "no_routes");
    descriptor.Routes = new List<string>();
    var descriptors = new List<PermissionDescriptor> { descriptor };

    var result = _service.ValidateConsistency(descriptors);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
  }

  [Fact]
  public void ValidateConsistency_WithMissingHttpMethods_ShouldReturnValidationErrors()
  {
    var descriptor = CreatePermissionDescriptor("users.no_http", "Users", "no_http");
    descriptor.HttpMethods = new List<string>();
    var descriptors = new List<PermissionDescriptor> { descriptor };

    var result = _service.ValidateConsistency(descriptors);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
  }

  [Fact]
  public void ValidateConsistency_WithNullRoutes_ShouldReturnValidationErrors()
  {
    var descriptor = CreatePermissionDescriptor("users.null_routes", "Users", "null_routes");
    descriptor.Routes = null!;

    var result = _service.ValidateConsistency(new List<PermissionDescriptor> { descriptor });

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
  }

  [Fact]
  public void ValidateConsistency_WithNullHttpMethods_ShouldReturnValidationErrors()
  {
    var descriptor = CreatePermissionDescriptor("users.null_http", "Users", "null_http");
    descriptor.HttpMethods = null!;

    var result = _service.ValidateConsistency(new List<PermissionDescriptor> { descriptor });

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
  }

  [Fact]
  public void ValidateConsistency_WhenEnumerationThrows_ShouldReturnServerError()
  {
    var descriptors = new ThrowingPermissionDescriptorList();

    var result = _service.ValidateConsistency(descriptors);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status500Unknown);
    (result.Error ?? string.Empty).Should().Contain("Error validando consistencia");
  }

  [Fact]
  public void ValidateConsistency_WithNullDescriptors_ShouldThrowArgumentNullException()
  {
    ((Action)(() => _service.ValidateConsistency(null!))).Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void ValidateConsistency_WithEmptyList_ShouldReturnSuccess()
  {
    var emptyList = new List<PermissionDescriptor>();

    var result = _service.ValidateConsistency(emptyList);

    result.Success.Should().BeTrue();
    result.Data!.Should().NotBeNull();
    result.Data!.Should().BeEmpty();
  }

  [Theory]
  [InlineData("users.valid_action", "Users", "valid_action", true)]
  [InlineData("", "Users", "valid_action", false)]
  [InlineData("users.valid_action", "", "valid_action", false)]
  [InlineData("users.valid_action", "Users", "", false)]
  public void ValidateConsistency_WithDifferentFieldValues_ShouldValidateCorrectly(string key, string feature, string action, bool shouldBeValid)
  {
    var descriptors = new List<PermissionDescriptor>
        {
            CreatePermissionDescriptor(key, feature, action),
        };

    var result = _service.ValidateConsistency(descriptors);

    if (shouldBeValid)
    {
      result.Success.Should().BeTrue();
      result.Data!.Should().NotBeNull();
      result.Data!.Should().BeEmpty();
    }
    else
    {
      result.Success.Should().BeFalse();
      result.ErrorInfo.Should().NotBeNull();
      result.ErrorInfo!.Key.Should().Be("Status400Validation");
      result.Error.Should().NotBeNull();
    }
  }

  #region Helper Methods

  private static List<PermissionDescriptor> CreateValidPermissionDescriptors()
  {
    return new List<PermissionDescriptor>
        {
            CreatePermissionDescriptor("users.create_user", "Users", "create_user"),
            CreatePermissionDescriptor("posts.update_post", "Posts", "update_post"),
            CreatePermissionDescriptor("auth.login", "Auth", "login"),
        };
  }

  private static PermissionDescriptor CreatePermissionDescriptor(
      string key,
      string feature,
      string action,
      string cqrsType = "Command")
  {
    return new PermissionDescriptor
    {
      Key = key,
      Feature = feature,
      Action = action,
      Description = $"Test permission para {action}",
      CqrsType = cqrsType,
      SourceFilePath = $"/Features/{feature}/Commands/{action}/{action}EndPoint.cs",
      EndpointMethodName = $"Map{action}",
      HttpMethods = new List<string> { "POST" },
      Routes = new List<string> { $"/{feature.ToLower(CultureInfo.InvariantCulture)}" },
      IsObsolete = false,
      DiscoveredAt = DateTime.UtcNow,
      ContentHash = Guid.NewGuid().ToString(),
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string>(),
    };
  }

  private static Permission CreateExistingPermission(PermissionDescriptor descriptor, string? description = null)
  {
    return new Permission
    {
      Key = descriptor.Key,
      Description = description ?? descriptor.Description,
      HasPermissionsNotRequired = descriptor.HasPermissionsNotRequired,
      PermissionsNotRequiredReason = descriptor.PermissionsNotRequiredReason,
      RequiresDevelopmentAccess = descriptor.RequiresDevelopmentAccess,
      RequiresDevelopmentAccessReason = descriptor.RequiresDevelopmentAccessReason,
      RequiresSuAdminAccess = descriptor.RequiresSuAdminAccess,
      RequiresSuAdminAccessReason = descriptor.RequiresSuAdminAccessReason,
      HasRequirePermissions = descriptor.HasRequirePermissions,
      RequirePermissionsNote = descriptor.RequirePermissionsNote,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow,
    };
  }

  private static Permission CreateExistingPermissionWithChangedRequireNote(PermissionDescriptor descriptor)
  {
    var permission = CreateExistingPermission(descriptor, descriptor.Description);
    permission.RequirePermissionsNote = "different";
    return permission;
  }

  private sealed class ThrowingPermissionDescriptorList : IList<PermissionDescriptor>
  {
    public PermissionDescriptor this[int index] { get => throw new InvalidOperationException("boom"); set => throw new InvalidOperationException("boom"); }
    public int Count => 1;
    public bool IsReadOnly => true;
    public void Add(PermissionDescriptor item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(PermissionDescriptor item) => false;
    public void CopyTo(PermissionDescriptor[] array, int arrayIndex) => throw new NotSupportedException();
    public IEnumerator<PermissionDescriptor> GetEnumerator() => throw new InvalidOperationException("boom");
    public int IndexOf(PermissionDescriptor item) => -1;
    public void Insert(int index, PermissionDescriptor item) => throw new NotSupportedException();
    public bool Remove(PermissionDescriptor item) => throw new NotSupportedException();
    public void RemoveAt(int index) => throw new NotSupportedException();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
  }

  #endregion
}
