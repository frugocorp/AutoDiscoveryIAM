// -----------------------------------------------------------------------
// <copyright file="AuthorizationValidationTypesTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Common;

/// <summary>
/// Tests unitarios para la clase AuthorizationValidationTypes.
/// </summary>
public class AuthorizationValidationTypesTests
{
    [Fact]
    public void PermissionsNotRequired_ShouldCreateCorrectInstance()
    {
        // Arrange
        const string reason = "Endpoint público";

        // Act
        var result = AuthorizationValidationTypes.PermissionsNotRequired(reason);

        // Assert
        result.Type.Should().Be(AuthorizationType.PermissionsNotRequired);
        result.Reason.Should().Be(reason);
        result.SkipsPermissionValidation.Should().BeTrue();
        result.RequiresSpecialAdminAccess.Should().BeFalse();
        result.UsesStandardValidation.Should().BeFalse();
    }

    [Fact]
    public void DevelopmentAccess_ShouldCreateCorrectInstance()
    {
        // Arrange
        const string reason = "Funcionalidad de debugging";

        // Act
        var result = AuthorizationValidationTypes.DevelopmentAccess(reason);

        // Assert
        result.Type.Should().Be(AuthorizationType.DevelopmentAccess);
        result.Reason.Should().Be(reason);
        result.SkipsPermissionValidation.Should().BeFalse();
        result.RequiresSpecialAdminAccess.Should().BeTrue();
        result.UsesStandardValidation.Should().BeFalse();
    }

    [Fact]
    public void SuperAdminAccess_ShouldCreateCorrectInstance()
    {
        // Arrange
        const string reason = "Operación crítica del sistema";

        // Act
        var result = AuthorizationValidationTypes.SuperAdminAccess(reason);

        // Assert
        result.Type.Should().Be(AuthorizationType.SuperAdminAccess);
        result.Reason.Should().Be(reason);
        result.SkipsPermissionValidation.Should().BeFalse();
        result.RequiresSpecialAdminAccess.Should().BeTrue();
        result.UsesStandardValidation.Should().BeFalse();
    }

    [Fact]
    public void ExplicitValidation_ShouldCreateCorrectInstance()
    {
        // Arrange
        const string note = "Validación documentada explícitamente";

        // Act
        var result = AuthorizationValidationTypes.ExplicitValidation(note);

        // Assert
        result.Type.Should().Be(AuthorizationType.ExplicitValidation);
        result.Reason.Should().Be(note);
        result.SkipsPermissionValidation.Should().BeFalse();
        result.RequiresSpecialAdminAccess.Should().BeFalse();
        result.UsesStandardValidation.Should().BeTrue();
    }

    [Fact]
    public void StandardValidation_ShouldCreateCorrectInstance()
    {
        // Act
        var result = AuthorizationValidationTypes.StandardValidation();

        // Assert
        result.Type.Should().Be(AuthorizationType.StandardValidation);
        result.Reason.Should().BeNull();
        result.SkipsPermissionValidation.Should().BeFalse();
        result.RequiresSpecialAdminAccess.Should().BeFalse();
        result.UsesStandardValidation.Should().BeTrue();
    }

    [Fact]
    public void FromPermissionData_WithPermissionsNotRequired_ShouldReturnCorrectType()
    {
        // Arrange
        const string reason = "Endpoint público";

        // Act
        var result = AuthorizationValidationTypes.FromPermissionData(
            hasPermissionsNotRequired: true,
            permissionsNotRequiredReason: reason,
            requiresDevelopmentAccess: false,
            developmentAccessReason: null,
            requiresSuAdminAccess: false,
            suAdminAccessReason: null,
            hasExplicitValidation: false,
            explicitValidationNote: null);

        // Assert
        result.Type.Should().Be(AuthorizationType.PermissionsNotRequired);
        result.Reason.Should().Be(reason);
    }

    [Fact]
    public void FromPermissionData_WithDevelopmentAccess_ShouldReturnCorrectType()
    {
        // Arrange
        const string reason = "Funcionalidad de debugging";

        // Act
        var result = AuthorizationValidationTypes.FromPermissionData(
            hasPermissionsNotRequired: false,
            permissionsNotRequiredReason: null,
            requiresDevelopmentAccess: true,
            developmentAccessReason: reason,
            requiresSuAdminAccess: false,
            suAdminAccessReason: null,
            hasExplicitValidation: false,
            explicitValidationNote: null);

        // Assert
        result.Type.Should().Be(AuthorizationType.DevelopmentAccess);
        result.Reason.Should().Be(reason);
    }

    [Fact]
    public void FromPermissionData_WithSuperAdminAccess_ShouldReturnCorrectType()
    {
        // Arrange
        const string reason = "Operación crítica";

        // Act
        var result = AuthorizationValidationTypes.FromPermissionData(
            hasPermissionsNotRequired: false,
            permissionsNotRequiredReason: null,
            requiresDevelopmentAccess: false,
            developmentAccessReason: null,
            requiresSuAdminAccess: true,
            suAdminAccessReason: reason,
            hasExplicitValidation: false,
            explicitValidationNote: null);

        // Assert
        result.Type.Should().Be(AuthorizationType.SuperAdminAccess);
        result.Reason.Should().Be(reason);
    }

    [Fact]
    public void FromPermissionData_WithExplicitValidation_ShouldReturnCorrectType()
    {
        // Arrange
        const string note = "Validación explícita";

        // Act
        var result = AuthorizationValidationTypes.FromPermissionData(
            hasPermissionsNotRequired: false,
            permissionsNotRequiredReason: null,
            requiresDevelopmentAccess: false,
            developmentAccessReason: null,
            requiresSuAdminAccess: false,
            suAdminAccessReason: null,
            hasExplicitValidation: true,
            explicitValidationNote: note);

        // Assert
        result.Type.Should().Be(AuthorizationType.ExplicitValidation);
        result.Reason.Should().Be(note);
    }

    [Fact]
    public void FromPermissionData_WithNoSpecialFlags_ShouldReturnStandardValidation()
    {
        // Act
        var result = AuthorizationValidationTypes.FromPermissionData(
            hasPermissionsNotRequired: false,
            permissionsNotRequiredReason: null,
            requiresDevelopmentAccess: false,
            developmentAccessReason: null,
            requiresSuAdminAccess: false,
            suAdminAccessReason: null,
            hasExplicitValidation: false,
            explicitValidationNote: null);

        // Assert
        result.Type.Should().Be(AuthorizationType.StandardValidation);
        result.Reason.Should().BeNull();
    }

    [Fact]
    public void FromPermissionData_WithConflictingFlags_ShouldThrowException()
    {
        // Act & Assert
        var action = () => AuthorizationValidationTypes.FromPermissionData(
            hasPermissionsNotRequired: true,
            permissionsNotRequiredReason: "Público",
            requiresDevelopmentAccess: true,
            developmentAccessReason: "Dev",
            requiresSuAdminAccess: false,
            suAdminAccessReason: null,
            hasExplicitValidation: false,
            explicitValidationNote: null);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Un endpoint no puede tener múltiples tipos de autorización conflictivos*");
    }

    [Theory]
    [InlineData(AuthorizationType.PermissionsNotRequired, "Sin validación de permisos")]
    [InlineData(AuthorizationType.DevelopmentAccess, "Acceso de desarrollo")]
    [InlineData(AuthorizationType.SuperAdminAccess, "Acceso de SuperAdmin")]
    [InlineData(AuthorizationType.ExplicitValidation, "Validación explícita")]
    [InlineData(AuthorizationType.StandardValidation, "Validación estándar")]
    public void ToString_ShouldReturnExpectedDescription(AuthorizationType type, string expectedDescription)
    {
        // Arrange
        var instance = type switch
        {
            AuthorizationType.PermissionsNotRequired => AuthorizationValidationTypes.PermissionsNotRequired(),
            AuthorizationType.DevelopmentAccess => AuthorizationValidationTypes.DevelopmentAccess(),
            AuthorizationType.SuperAdminAccess => AuthorizationValidationTypes.SuperAdminAccess(),
            AuthorizationType.ExplicitValidation => AuthorizationValidationTypes.ExplicitValidation(),
            AuthorizationType.StandardValidation => AuthorizationValidationTypes.StandardValidation(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        // Act
        var result = instance.ToString();

        // Assert
        result.Should().Be(expectedDescription);
    }

    [Fact]
    public void ToString_WithReason_ShouldIncludeReasonInOutput()
    {
        // Arrange
        const string reason = "Endpoint de testing";
        var instance = AuthorizationValidationTypes.PermissionsNotRequired(reason);

        // Act
        var result = instance.ToString();

        // Assert
        result.Should().Be($"Sin validación de permisos: {reason}");
    }

    [Theory]
    [InlineData(true, false, false, false, true)] // PermissionsNotRequired
    [InlineData(false, true, false, false, false)] // DevelopmentAccess
    [InlineData(false, false, true, false, false)] // SuperAdminAccess
    [InlineData(false, false, false, true, false)] // ExplicitValidation
    [InlineData(false, false, false, false, false)] // StandardValidation
    public void SkipsPermissionValidation_ShouldReturnExpectedValue(
        bool hasPermissionsNotRequired,
        bool requiresDevelopmentAccess,
        bool requiresSuAdminAccess,
        bool hasExplicitValidation,
        bool expectedSkips)
    {
        // Act
        var result = AuthorizationValidationTypes.FromPermissionData(
            hasPermissionsNotRequired,
            null,
            requiresDevelopmentAccess,
            null,
            requiresSuAdminAccess,
            null,
            hasExplicitValidation,
            null);

        // Assert
        result.SkipsPermissionValidation.Should().Be(expectedSkips);
    }
}



