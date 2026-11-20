// -----------------------------------------------------------------------
// <copyright file="LocalizedAuthorizationMessagesTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using AwesomeAssertions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Common;

/// <summary>
/// Tests para verificar que los mensajes de autorización están localizados correctamente.
/// </summary>
public class LocalizedAuthorizationMessagesTests
{
    [Fact]
    public void AuthorizationMessages_ShouldBeInSpanish()
    {
        // Act & Assert - Verificar que todos los mensajes estén en español
        AuthorizationValidationTypes.Messages.AuthenticationRequired
            .Should().Be("Se requiere autenticación para acceder a este recurso");

        AuthorizationValidationTypes.Messages.InvalidUserContext
            .Should().Be("Contexto de usuario inválido - no se pudo obtener la información del usuario");

        AuthorizationValidationTypes.Messages.DevelopmentAccessRequired
            .Should().Be("Se requiere acceso de desarrollo para este recurso");

        AuthorizationValidationTypes.Messages.SuperAdminAccessRequired
            .Should().Be("Se requiere acceso de SuperAdmin para este recurso");

        AuthorizationValidationTypes.Messages.InsufficientPermissions
            .Should().Be("Permisos insuficientes para acceder a este recurso");

        AuthorizationValidationTypes.Messages.PermissionValidationFailed
            .Should().Be("La validación de permisos falló - no se pudo determinar el permiso requerido");
    }

    [Fact]
    public void AuthorizationMessages_ShouldNotBeEmpty()
    {
        // Act & Assert - Verificar que ningún mensaje esté vacío
        AuthorizationValidationTypes.Messages.AuthenticationRequired
            .Should().NotBeNullOrWhiteSpace();

        AuthorizationValidationTypes.Messages.InvalidUserContext
            .Should().NotBeNullOrWhiteSpace();

        AuthorizationValidationTypes.Messages.DevelopmentAccessRequired
            .Should().NotBeNullOrWhiteSpace();

        AuthorizationValidationTypes.Messages.SuperAdminAccessRequired
            .Should().NotBeNullOrWhiteSpace();

        AuthorizationValidationTypes.Messages.InsufficientPermissions
            .Should().NotBeNullOrWhiteSpace();

        AuthorizationValidationTypes.Messages.PermissionValidationFailed
            .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AuthorizationMessages_ShouldHaveProperFormat()
    {
        // Act & Assert - Verificar que los mensajes tengan formato correcto
        var messages = new[]
        {
            AuthorizationValidationTypes.Messages.AuthenticationRequired,
            AuthorizationValidationTypes.Messages.InvalidUserContext,
            AuthorizationValidationTypes.Messages.DevelopmentAccessRequired,
            AuthorizationValidationTypes.Messages.SuperAdminAccessRequired,
            AuthorizationValidationTypes.Messages.InsufficientPermissions,
            AuthorizationValidationTypes.Messages.PermissionValidationFailed,
        };

        foreach (var message in messages)
        {
            // Los mensajes no deben contener caracteres especiales problemáticos
            message.Should().NotContain("{{").And.NotContain("}}");

            // Los mensajes deben comenzar con mayúscula
            char.IsUpper(message[0]).Should().BeTrue();

            // Los mensajes deben ser razonablemente largos (no menos de 10 caracteres)
            message.Length.Should().BeGreaterThan(10);
        }
    }

    [Theory]
    [InlineData("MyPermission")]
    [InlineData("users:create")]
    [InlineData("posts:update:own")]
    public void PermissionRequired_Template_ShouldFormatCorrectly(string permissionKey)
    {
        // Arrange & Act
        var formattedMessage = $"{AuthorizationValidationTypes.Messages.InsufficientPermissions}. Requerido: {permissionKey}";

        // Assert
        formattedMessage.Should().Contain(permissionKey);
        formattedMessage.Should().StartWith("Permisos insuficientes");
        formattedMessage.Should().Contain("Requerido:");
    }
}



