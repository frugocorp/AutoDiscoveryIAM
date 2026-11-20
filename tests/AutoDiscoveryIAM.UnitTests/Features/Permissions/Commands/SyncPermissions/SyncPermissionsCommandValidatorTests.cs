// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsEndPointTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Tests unitarios para SyncPermissionsCommandValidator.
/// </summary>
public class SyncPermissionsCommandValidatorTests
{
  private readonly SyncPermissionsCommandValidator _validator;

  public SyncPermissionsCommandValidatorTests()
  {
    _validator = new SyncPermissionsCommandValidator();
  }

  [Fact]
  public void Validate_WithValidCommand_ShouldNotHaveValidationErrors()
  {
    // Arrange
    var request = new SyncPermissionsRequestDto(ForceFullSync: false, DryRun: false);
    var command = new SyncPermissionsCommand(request);

    // Act
    var result = _validator.TestValidate(command);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  public void Validate_WithForceFullSyncTrue_ShouldNotHaveValidationErrors()
  {
    // Arrange
    var request = new SyncPermissionsRequestDto(ForceFullSync: true, DryRun: false);
    var command = new SyncPermissionsCommand(request);

    // Act
    var result = _validator.TestValidate(command);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  public void Validate_WithNullRequest_ShouldHaveValidationError()
  {
    // Arrange
    var command = new SyncPermissionsCommand(null!);

    // Act
    var result = _validator.TestValidate(command);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Request);
  }

  [Theory]
  [InlineData(true, false)]
  [InlineData(false, true)]
  [InlineData(false, false)]
  public void Validate_WithDifferentParameterValues_ShouldNotHaveValidationErrors(bool forceFullSync, bool dryRun)
  {
    // Arrange
    var request = new SyncPermissionsRequestDto(ForceFullSync: forceFullSync, DryRun: dryRun);
    var command = new SyncPermissionsCommand(request);

    // Act
    var result = _validator.TestValidate(command);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  public void Validate_WithForceFullSyncAndDryRunBothTrue_ShouldHaveValidationError()
  {
    // Arrange
    var request = new SyncPermissionsRequestDto(ForceFullSync: true, DryRun: true);
    var command = new SyncPermissionsCommand(request);

    // Act
    var result = _validator.TestValidate(command);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Request)
        .WithErrorMessage("ForceFullSync no tiene efecto cuando DryRun está habilitado");
  }

  [Fact]
  public void Validate_WithDryRunTrue_ShouldNotHaveValidationErrors()
  {
    // Arrange
    var request = new SyncPermissionsRequestDto(ForceFullSync: false, DryRun: true);
    var command = new SyncPermissionsCommand(request);

    // Act
    var result = _validator.TestValidate(command);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }
}
