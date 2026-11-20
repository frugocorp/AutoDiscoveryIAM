// -----------------------------------------------------------------------
// <copyright file="PermissionNamingServiceTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Services.PermissionNaming;

/// <summary>
/// Tests unitarios para PermissionNamingService.
/// Verifica la conversión correcta de namespaces a permission keys.
/// Utiliza aserciones homogéneas con AwesomeAssertions.
/// </summary>
public class PermissionNamingServiceTests
{
  private readonly PermissionNamingService _service;

  public PermissionNamingServiceTests()
  {
    _service = new PermissionNamingService();
  }

  #region BuildPermissionNaming Tests

  [Theory]
  [InlineData("FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Posts.Commands.CreatePost", "posts", "CreatePost", "create_post", "Command", "posts.create_post")]
  [InlineData("FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers", "users", "GetUsers", "get_users", "Query", "users.get_users")]
  [InlineData("FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId", "roles", "RestoreRoleByPublicId", "restore_role_by_public_id", "Command", "roles.restore_role_by_public_id")]
  public void BuildPermissionNaming_WithValidNamespace_ShouldReturnValidResult(
      string namespaceName,
      string expectedFeature,
      string expectedAction,
      string expectedActionSnakeCase,
      string expectedCqrsType,
      string expectedKey)
  {
    // Act
    var result = _service.BuildPermissionNaming(namespaceName);

    // Assert
    result.IsValid.Should().BeTrue($"Se esperaba un resultado válido para el namespace: {namespaceName}");
    result.ErrorMessage.Should().BeNull();
    result.Feature.Should().Be(expectedFeature);
    result.Action.Should().Be(expectedAction);
    result.ActionSnakeCase.Should().Be(expectedActionSnakeCase);
    result.CqrsType.Should().Be(expectedCqrsType);
    result.Key.Should().Be(expectedKey);
  }

  [Theory]
  [InlineData("FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Posts")]
  [InlineData("MiApi.Features.Posts")]
  [InlineData("Features.Posts")]
  public void BuildPermissionNaming_WithInvalidNamespace_ShouldReturnErrorResult(string namespaceName)
  {
    // Act
    var result = _service.BuildPermissionNaming(namespaceName);

    // Assert
    result.IsValid.Should().BeFalse($"Se esperaba un resultado inválido para el namespace: {namespaceName}");
    result.ErrorMessage.Should().NotBeNull();
    result.ErrorMessage.Should().NotBeEmpty();
    result.ErrorMessage.Should().Contain("too short");
  }

  /// <summary>
  /// Namespaces de la app que consume el paquete: el root es libre (un segmento como el que genera
  /// <c>dotnet new webapi -n MiApi</c>, dos, o tres como el de la propia librería). Antes se exigía
  /// la misma cantidad de segmentos que el nombre de la librería y <c>MiApi</c> se descartaba sin aviso.
  /// </summary>
  [Theory]
  [InlineData("MiApi.Features.Pedidos.Commands.CrearPedido", "pedidos.crear_pedido", "Command")]
  [InlineData("Empresa.Tienda.Features.Pedidos.Queries.GetPedidos", "pedidos.get_pedidos", "Query")]
  [InlineData("FrugoCorp.AspNetCore.Tienda.Features.Pedidos.Commands.CrearPedido", "pedidos.crear_pedido", "Command")]
  [InlineData("MiApi.Features.Reportes.GenerarReporte", "reportes.generar_reporte", "Unknown")]
  [InlineData("Features.Pedidos.CrearPedido", "pedidos.crear_pedido", "Unknown")]
  public void BuildPermissionNaming_WithConsumerNamespace_ShouldReturnValidResult(string namespaceName, string expectedKey, string expectedCqrsType)
  {
    // Act
    var result = _service.BuildPermissionNaming(namespaceName);

    // Assert
    result.IsValid.Should().BeTrue($"Se esperaba un resultado válido para el namespace: {namespaceName} ({result.ErrorMessage})");
    result.Key.Should().Be(expectedKey);
    result.CqrsType.Should().Be(expectedCqrsType);
  }

  /// <summary>
  /// Los slices anidados de la librería son permisos propios (la acción es el último segmento):
  /// sus claves no deben cambiar.
  /// </summary>
  [Theory]
  [InlineData("FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode", "auth.exchange_login_code")]
  [InlineData("FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google", "auth.google")]
  [InlineData("FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor", "auth.verify_two_factor")]
  public void BuildPermissionNaming_WithNestedSlice_ShouldUseLastSegmentAsAction(string namespaceName, string expectedKey)
  {
    // Act
    var result = _service.BuildPermissionNaming(namespaceName);

    // Assert
    result.IsValid.Should().BeTrue();
    result.Key.Should().Be(expectedKey);
  }

  [Theory]
  [InlineData("")]
  [InlineData("short")]
  [InlineData("too.short.namespace")]
  [InlineData("MiApi.Pedidos.Endpoints")]
  [InlineData("MiApi.Features.Users.Commands")]
  [InlineData("MiApi.Features.Users.Queries")]
  public void BuildPermissionNaming_WithoutFeatureOrAction_ShouldReturnErrorResult(string namespaceName)
  {
    // Act
    var result = _service.BuildPermissionNaming(namespaceName);

    // Assert
    result.IsValid.Should().BeFalse($"Se esperaba un resultado inválido para el namespace: {namespaceName}");
    result.ErrorMessage.Should().Contain("Could not extract feature or action");
  }

  [Fact]
  public void BuildPermissionNaming_WithMissingFeatureInNamespace_ShouldReturnErrorResult()
  {
    // Arrange - namespace sin la parte "Features"
    const string namespaceName = "FrugoCorp.AspNetCore.AutoDiscoveryIAM.SomeOther.Commands.CreatePost";

    // Act
    var result = _service.BuildPermissionNaming(namespaceName);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().NotBeNull();
    result.ErrorMessage.Should().Contain("Could not extract feature or action");
  }

  #endregion

  #region ParseNamespaceParts Tests

  [Fact]
  public void ParseNamespaceParts_WithValidDottedString_ShouldReturnCorrectParts()
  {
    // Arrange
    const string input = "a.b.c.d";
    var expectedParts = new[] { "a", "b", "c", "d" };

    // Act
    var result = _service.ParseNamespaceParts(input);

    // Assert
    result.Length.Should().Be(expectedParts.Length);
    for (int i = 0; i < expectedParts.Length; i++)
    {
      result[i].Should().Be(expectedParts[i]);
    }
  }

  [Fact]
  public void ParseNamespaceParts_WithSinglePart_ShouldReturnSingleElement()
  {
    // Arrange
    const string input = "single";

    // Act
    var result = _service.ParseNamespaceParts(input);

    // Assert
    result.Should().ContainSingle();
    result[0].Should().Be("single");
  }

  [Fact]
  public void ParseNamespaceParts_WithEmptyString_ShouldReturnArrayWithEmptyString()
  {
    // Arrange
    const string input = "";

    // Act
    var result = _service.ParseNamespaceParts(input);

    // Assert - string.Split("") devuelve [""] y no un arreglo vacío
    result.Should().ContainSingle();
    result[0].Should().Be("");
  }

  [Fact]
  public void ParseNamespaceParts_WithNull_ShouldReturnEmptyArray()
  {
    // Act
    var result = _service.ParseNamespaceParts(null);

    // Assert
    result.Should().BeEmpty();
  }

  #endregion

  #region ExtractFeatureFromNamespace Tests

  [Fact]
  public void ExtractFeatureFromNamespace_WithValidFeaturesStructure_ShouldReturnCorrectFeature()
  {
    // Arrange
    var namespaceParts = new[] { "FrugoCorp.AspNetCore.AutoDiscoveryIAM", "Features", "Posts", "Commands", "CreatePost" };

    // Act
    var result = _service.ExtractFeatureFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("Posts");
  }

  [Fact]
  public void ExtractFeatureFromNamespace_WithUsersFeature_ShouldReturnUsers()
  {
    // Arrange
    var namespaceParts = new[] { "app", "Features", "Users", "Queries", "GetUsers" };

    // Act
    var result = _service.ExtractFeatureFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("Users");
  }

  [Fact]
  public void ExtractFeatureFromNamespace_WithoutFeatures_ShouldReturnEmpty()
  {
    // Arrange
    var namespaceParts = new[] { "no", "features", "here" };

    // Act
    var result = _service.ExtractFeatureFromNamespace(namespaceParts);

    // Assert
    result.Should().Be(string.Empty);
  }

  [Fact]
  public void ExtractFeatureFromNamespace_WithFeaturesAtEnd_ShouldReturnEmpty()
  {
    // Arrange - Features al final, sin elemento siguiente
    var namespaceParts = new[] { "Features" };

    // Act
    var result = _service.ExtractFeatureFromNamespace(namespaceParts);

    // Assert
    result.Should().Be(string.Empty);
  }

  [Fact]
  public void ExtractFeatureFromNamespace_WithEmptyArray_ShouldReturnEmpty()
  {
    // Arrange
    var namespaceParts = Array.Empty<string>();

    // Act
    var result = _service.ExtractFeatureFromNamespace(namespaceParts);

    // Assert
    result.Should().Be(string.Empty);
  }

  #endregion

  #region ExtractActionFromNamespace Tests

  [Fact]
  public void ExtractActionFromNamespace_WithMultipleParts_ShouldReturnLastPart()
  {
    // Arrange
    var namespaceParts = new[] { "a", "b", "c", "CreatePost" };

    // Act
    var result = _service.ExtractActionFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("CreatePost");
  }

  [Fact]
  public void ExtractActionFromNamespace_WithSinglePart_ShouldReturnThatPart()
  {
    // Arrange
    var namespaceParts = new[] { "GetUsers" };

    // Act
    var result = _service.ExtractActionFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("GetUsers");
  }

  [Fact]
  public void ExtractActionFromNamespace_WithEmptyArray_ShouldReturnEmpty()
  {
    // Act
    var result = _service.ExtractActionFromNamespace(Array.Empty<string>());

    // Assert
    result.Should().Be(string.Empty);
  }

  #endregion

  #region ExtractCqrsTypeFromNamespace Tests

  [Fact]
  public void ExtractCqrsTypeFromNamespace_WithCommands_ShouldReturnCommand()
  {
    // Arrange
    var namespaceParts = new[] { "app", "Features", "Posts", "Commands", "CreatePost" };

    // Act
    var result = _service.ExtractCqrsTypeFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("Command");
  }

  [Fact]
  public void ExtractCqrsTypeFromNamespace_WithQueries_ShouldReturnQuery()
  {
    // Arrange
    var namespaceParts = new[] { "app", "Features", "Users", "Queries", "GetUsers" };

    // Act
    var result = _service.ExtractCqrsTypeFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("Query");
  }

  [Fact]
  public void ExtractCqrsTypeFromNamespace_WithoutCqrsTypes_ShouldReturnUnknown()
  {
    // Arrange
    var namespaceParts = new[] { "no", "cqrs", "here" };

    // Act
    var result = _service.ExtractCqrsTypeFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("Unknown");
  }

  [Fact]
  public void ExtractCqrsTypeFromNamespace_WithEmptyArray_ShouldReturnUnknown()
  {
    // Arrange
    var namespaceParts = Array.Empty<string>();

    // Act
    var result = _service.ExtractCqrsTypeFromNamespace(namespaceParts);

    // Assert
    result.Should().Be("Unknown");
  }

  #endregion

  #region ConvertToSnakeCase Tests

  [Theory]
  [InlineData("CreatePost", "create_post")]
  [InlineData("GetUsers", "get_users")]
  [InlineData("RestoreRoleByPublicId", "restore_role_by_public_id")]
  [InlineData("UpdateUserById", "update_user_by_id")]
  [InlineData("GetPostsByUserId", "get_posts_by_user_id")]
  [InlineData("simple", "simple")]
  [InlineData("ALLCAPS", "allcaps")]
  public void ConvertToSnakeCase_WithVariousPascalCaseInputs_ShouldReturnSnakeCase(string input, string expected)
  {
    // Act
    var result = _service.ConvertToSnakeCase(input);

    // Assert
    result.Should().Be(expected);
  }

  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void ConvertToSnakeCase_WithNullOrEmpty_ShouldReturnEmpty(string? input)
  {
    // Act
    var result = _service.ConvertToSnakeCase(input!);

    // Assert
    result.Should().Be(string.Empty);
  }

  #endregion

  #region GeneratePermissionKey Tests

  [Theory]
  [InlineData("Posts", "create_post", "posts.create_post")]
  [InlineData("USERS", "get_users", "users.get_users")]
  [InlineData("Roles", "restore_role_by_public_id", "roles.restore_role_by_public_id")]
  public void GeneratePermissionKey_WithVariousInputs_ShouldReturnCorrectKey(string feature, string actionSnakeCase, string expectedKey)
  {
    // Act
    var result = _service.GeneratePermissionKey(feature, actionSnakeCase);

    // Assert
    result.Should().Be(expectedKey);
  }

  #endregion
}
