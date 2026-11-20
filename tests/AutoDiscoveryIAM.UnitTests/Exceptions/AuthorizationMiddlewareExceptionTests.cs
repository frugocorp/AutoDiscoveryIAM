// -----------------------------------------------------------------------
// <copyright file="AuthorizationMiddlewareExceptionTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Exceptions;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Exceptions;

/// <summary>
/// Tests unitarios para la clase AuthorizationMiddlewareException.
/// </summary>
public class AuthorizationMiddlewareExceptionTests
{
    [Fact]
    public void AuthenticationRequired_ShouldCreateCorrectException()
    {
        // Arrange
        var validationType = AuthorizationValidationTypes.StandardValidation();
        const string endpointPath = "/api/users";
        const string httpMethod = "GET";
        const string reason = "Token JWT inválido";

        // Act
        var exception = AuthorizationMiddlewareException.AuthenticationRequired(
            validationType, endpointPath, httpMethod, reason);

        // Assert
        exception.ValidationType.Should().Be(validationType);
        exception.EndpointPath.Should().Be(endpointPath);
        exception.HttpMethod.Should().Be(httpMethod);
        exception.UserId.Should().BeNull();
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Message.Should().Contain("Autenticación requerida");
        exception.Message.Should().Contain(reason);
        exception.ContextData["FailureType"].Should().Be("AuthenticationRequired");
        exception.ContextData["Reason"].Should().Be(reason);
    }

    [Fact]
    public void InsufficientPermissions_ShouldCreateCorrectException()
    {
        // Arrange
        var validationType = AuthorizationValidationTypes.ExplicitValidation();
        const string endpointPath = "/api/admin/users";
        const string httpMethod = "DELETE";
        const string userId = "user123";
        var requiredPermissions = new List<string> { "users.delete", "admin.access" };
        const string reason = "Falta permiso de administrador";

        // Act
        var exception = AuthorizationMiddlewareException.InsufficientPermissions(
            validationType, endpointPath, httpMethod, userId, requiredPermissions, reason);

        // Assert
        exception.ValidationType.Should().Be(validationType);
        exception.EndpointPath.Should().Be(endpointPath);
        exception.HttpMethod.Should().Be(httpMethod);
        exception.UserId.Should().Be(userId);
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Message.Should().Contain("Permisos insuficientes");
        exception.Message.Should().Contain(userId);
        exception.Message.Should().Contain(reason);
        exception.ContextData["FailureType"].Should().Be("InsufficientPermissions");
        exception.ContextData["UserId"].Should().Be(userId);
        exception.ContextData["RequiredPermissions"].Should().BeEquivalentTo(requiredPermissions);
        exception.ContextData["Reason"].Should().Be(reason);
    }

    [Fact]
    public void DevelopmentAccessDenied_ShouldCreateCorrectException()
    {
        // Arrange
        const string endpointPath = "/api/debug/performance";
        const string httpMethod = "POST";
        const string userId = "user456";
        const string reason = "Endpoint solo para desarrollo";

        // Act
        var exception = AuthorizationMiddlewareException.DevelopmentAccessDenied(
            endpointPath, httpMethod, userId, reason);

        // Assert
        exception.ValidationType.Type.Should().Be(AuthorizationType.DevelopmentAccess);
        exception.ValidationType.Reason.Should().Be(reason);
        exception.EndpointPath.Should().Be(endpointPath);
        exception.HttpMethod.Should().Be(httpMethod);
        exception.UserId.Should().Be(userId);
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Message.Should().Contain("Acceso de desarrollo requerido");
        exception.Message.Should().Contain(userId);
        exception.Message.Should().Contain(reason);
        exception.ContextData["FailureType"].Should().Be("DevelopmentAccessDenied");
        exception.ContextData["RequiredAccess"].Should().Be("Development");
        exception.ContextData["UserId"].Should().Be(userId);
        exception.ContextData["Reason"].Should().Be(reason);
    }

    [Fact]
    public void DevelopmentAccessDenied_WithoutUserId_ShouldCreateCorrectException()
    {
        // Arrange
        const string endpointPath = "/api/debug/performance";
        const string httpMethod = "POST";
        const string reason = "Endpoint solo para desarrollo";

        // Act
        var exception = AuthorizationMiddlewareException.DevelopmentAccessDenied(
            endpointPath, httpMethod, reason: reason);

        // Assert
        exception.UserId.Should().BeNull();
        exception.Message.Should().Contain("usuario no autenticado");
        exception.ContextData.Should().NotContainKey("UserId");
    }

    [Fact]
    public void SuperAdminAccessDenied_ShouldCreateCorrectException()
    {
        // Arrange
        const string endpointPath = "/api/system/shutdown";
        const string httpMethod = "POST";
        const string userId = "admin789";
        const string reason = "Operación crítica del sistema";

        // Act
        var exception = AuthorizationMiddlewareException.SuperAdminAccessDenied(
            endpointPath, httpMethod, userId, reason);

        // Assert
        exception.ValidationType.Type.Should().Be(AuthorizationType.SuperAdminAccess);
        exception.ValidationType.Reason.Should().Be(reason);
        exception.EndpointPath.Should().Be(endpointPath);
        exception.HttpMethod.Should().Be(httpMethod);
        exception.UserId.Should().Be(userId);
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Message.Should().Contain("Acceso de SuperAdmin requerido");
        exception.Message.Should().Contain(userId);
        exception.Message.Should().Contain(reason);
        exception.ContextData["FailureType"].Should().Be("SuperAdminAccessDenied");
        exception.ContextData["RequiredAccess"].Should().Be("SuperAdmin");
        exception.ContextData["UserId"].Should().Be(userId);
        exception.ContextData["Reason"].Should().Be(reason);
    }

    [Fact]
    public void ConfigurationError_ShouldCreateCorrectException()
    {
        // Arrange
        const string endpointPath = "/api/malformed";
        const string httpMethod = "GET";
        const string configurationIssue = "Múltiples atributos de autorización conflictivos";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = AuthorizationMiddlewareException.ConfigurationError(
            endpointPath, httpMethod, configurationIssue, innerException);

        // Assert
        exception.ValidationType.Type.Should().Be(AuthorizationType.StandardValidation);
        exception.EndpointPath.Should().Be(endpointPath);
        exception.HttpMethod.Should().Be(httpMethod);
        exception.UserId.Should().BeNull();
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Message.Should().Contain("Error de configuración de autorización");
        exception.Message.Should().Contain(configurationIssue);
        exception.InnerException.Should().Be(innerException);
        exception.ContextData["FailureType"].Should().Be("ConfigurationError");
        exception.ContextData["ConfigurationIssue"].Should().Be(configurationIssue);
    }

    [Fact]
    public void GetLoggingContext_ShouldReturnCompleteInformation()
    {
        // Arrange
        var validationType = AuthorizationValidationTypes.DevelopmentAccess("Debugging endpoint");
        const string endpointPath = "/api/debug";
        const string httpMethod = "GET";
        const string userId = "dev123";

        var exception = AuthorizationMiddlewareException.DevelopmentAccessDenied(
            endpointPath, httpMethod, userId, "Propósito de prueba");

        // Act
        var context = exception.GetLoggingContext();

        // Assert
        context.Should().ContainKey("Exception");
        context.Should().ContainKey("ValidationType");
        context.Should().ContainKey("ValidationReason");
        context.Should().ContainKey("EndpointPath");
        context.Should().ContainKey("HttpMethod");
        context.Should().ContainKey("StatusCode");
        context.Should().ContainKey("Message");
        context.Should().ContainKey("UserId");
        context.Should().ContainKey("FailureType");
        context.Should().ContainKey("RequiredAccess");
        context.Should().ContainKey("Reason");

        context["Exception"].Should().Be("AuthorizationMiddlewareException");
        context["ValidationType"].Should().Be("DevelopmentAccess");
        context["EndpointPath"].Should().Be(endpointPath);
        context["HttpMethod"].Should().Be(httpMethod);
        context["StatusCode"].Should().Be((int)HttpStatusCode.Forbidden);
        context["UserId"].Should().Be(userId);
    }

    [Fact]
    public void ToString_ShouldReturnDetailedInformation()
    {
        // Arrange
        var validationType = AuthorizationValidationTypes.SuperAdminAccess("System operation");
        const string endpointPath = "/api/system";
        const string httpMethod = "DELETE";
        const string userId = "admin456";

        var exception = AuthorizationMiddlewareException.SuperAdminAccessDenied(
            endpointPath, httpMethod, userId, "Critical operation");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Acceso de SuperAdmin requerido");
        result.Should().Contain($"Endpoint: {httpMethod} {endpointPath}");
        result.Should().Contain($"Usuario: {userId}");
        result.Should().Contain("Status: 403 Forbidden");
        result.Should().Contain("Tipo: Acceso de SuperAdmin: Critical operation");
    }

    [Theory]
    [InlineData(AuthorizationType.StandardValidation, HttpStatusCode.Forbidden)]
    [InlineData(AuthorizationType.ExplicitValidation, HttpStatusCode.Forbidden)]
    [InlineData(AuthorizationType.DevelopmentAccess, HttpStatusCode.Forbidden)]
    [InlineData(AuthorizationType.SuperAdminAccess, HttpStatusCode.Forbidden)]
    public void StatusCode_ShouldBeCorrectForValidationType(AuthorizationType type, HttpStatusCode expectedStatus)
    {
        // Arrange
        var validationType = type switch
        {
            AuthorizationType.StandardValidation => AuthorizationValidationTypes.StandardValidation(),
            AuthorizationType.ExplicitValidation => AuthorizationValidationTypes.ExplicitValidation(),
            AuthorizationType.DevelopmentAccess => AuthorizationValidationTypes.DevelopmentAccess(),
            AuthorizationType.SuperAdminAccess => AuthorizationValidationTypes.SuperAdminAccess(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        // Act
        var exception = type switch
        {
            AuthorizationType.DevelopmentAccess => AuthorizationMiddlewareException.DevelopmentAccessDenied("/test", "GET"),
            AuthorizationType.SuperAdminAccess => AuthorizationMiddlewareException.SuperAdminAccessDenied("/test", "GET"),
            _ => AuthorizationMiddlewareException.InsufficientPermissions(validationType, "/test", "GET", "user123"),
        };

        // Assert
        exception.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public void ContextData_ShouldBeModifiable()
    {
        // Arrange
        var exception = AuthorizationMiddlewareException.AuthenticationRequired(
            AuthorizationValidationTypes.StandardValidation(), "/test", "GET");

        // Act
        exception.ContextData["CustomKey"] = "CustomValue";

        // Assert
        exception.ContextData["CustomKey"].Should().Be("CustomValue");
    }
}



