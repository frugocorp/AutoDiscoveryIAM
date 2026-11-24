// -----------------------------------------------------------------------
// <copyright file="EndpointDiscoveryConsumerNamespaceTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// Issue #103: el descubrimiento con endpoints de la app que consume el paquete, usando el
/// <see cref="PermissionNamingService"/> real (no un mock) y handlers declarados en namespaces de
/// consumidor (ver <c>ConsumerEndpointFixtures.cs</c>).
/// </summary>
public class EndpointDiscoveryConsumerNamespaceTests
{
  [Fact]
  public async Task DiscoverEndpointsFromMetadataAsync_WithSingleSegmentRootNamespace_ShouldDiscoverPermission()
  {
    var sut = CreateSut(Endpoint("/pedidos", "POST", typeof(MiApi.Features.Pedidos.Commands.CrearPedido.CrearPedidoEndpoint)));

    var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

    result.Should().ContainSingle();
    result.Single().Key.Should().Be("pedidos.crear_pedido");
    result.Single().SourceNamespace.Should().Be("MiApi.Features.Pedidos.Commands.CrearPedido");
  }

  [Fact(DisplayName = "Una descripción de endpoint más larga que la columna se recorta para que el sync no falle al guardar")]
  public async Task DiscoverEndpointsFromMetadataAsync_WithLongDescription_TruncatesToColumnLength()
  {
    var longDescription = new string('d', 800);
    var sut = CreateSut(Endpoint("/pedidos", "POST", typeof(MiApi.Features.Pedidos.Commands.CrearPedido.CrearPedidoEndpoint), metadata: new Microsoft.AspNetCore.Http.EndpointDescriptionAttribute(longDescription)));

    var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

    result.Single().Description!.Length.Should().Be(500);
    result.Single().Description!.EndsWith('…').Should().BeTrue();
  }

  /// <summary>
  /// Varias rutas del mismo slice (mismo namespace) siguen siendo un solo permiso, como
  /// <c>TwoFactor</c> o <c>PasswordManagement</c> en la librería.
  /// </summary>
  [Fact]
  public async Task DiscoverEndpointsFromMetadataAsync_WithSeveralRoutesInSameSlice_ShouldMergeIntoOnePermission()
  {
    var slice = typeof(MiApi.Features.Pedidos.Commands.CrearPedido.CrearPedidoEndpoint);
    var sut = CreateSut(
      Endpoint("/pedidos", "POST", slice, nameof(MiApi.Features.Pedidos.Commands.CrearPedido.CrearPedidoEndpoint.Handle)),
      Endpoint("/pedidos/borrador", "POST", slice, nameof(MiApi.Features.Pedidos.Commands.CrearPedido.CrearPedidoEndpoint.HandleOtraRuta)));

    var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

    result.Should().ContainSingle();
    string.Join(",", result.Single().Routes.Order()).Should().Be("/pedidos,/pedidos/borrador");
  }

  /// <summary>
  /// Dos slices distintos con la misma clave NO se fusionan: antes, quien tenía el permiso de uno
  /// entraba también al otro sin que nadie lo decidiera.
  /// </summary>
  [Fact]
  public async Task DiscoverEndpointsFromMetadataAsync_WithSameKeyFromDifferentNamespaces_ShouldNotMerge()
  {
    var sut = CreateSut(
      Endpoint("/users", "POST", typeof(MiApi.Features.Users.Commands.CreateUser.CreateUserEndpoint)),
      Endpoint("/modulo/users", "POST", typeof(OtraApi.Modulo.Features.Users.Commands.CreateUser.CreateUserEndpoint)));

    var result = await sut.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);

    result.Should().HaveCount(2);
    result.All(d => d.Key == "users.create_user").Should().BeTrue();
    result.Select(d => d.SourceNamespace).Distinct().Count().Should().Be(2);
  }

  [Fact]
  public async Task ValidateConsistency_WithSameKeyFromDifferentNamespaces_ShouldFailNamingBothNamespaces()
  {
    var endpointDiscovery = CreateSut(
      Endpoint("/users", "POST", typeof(MiApi.Features.Users.Commands.CreateUser.CreateUserEndpoint)),
      Endpoint("/modulo/users", "POST", typeof(OtraApi.Modulo.Features.Users.Commands.CreateUser.CreateUserEndpoint)));
    var descriptors = await endpointDiscovery.DiscoverEndpointsFromMetadataAsync(CancellationToken.None);
    var discoveryService = new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService.DiscoveryPermissionsService(
      Substitute.For<IPermissionLogger>(),
      endpointDiscovery,
      Substitute.For<ISyncPermissionsRepository>());

    var result = discoveryService.ValidateConsistency(descriptors);

    result.Success.Should().BeFalse();
    result.Error.Should().Contain("users.create_user");
    result.Error.Should().Contain("MiApi.Features.Users.Commands.CreateUser");
    result.Error.Should().Contain("OtraApi.Modulo.Features.Users.Commands.CreateUser");
  }

  [Fact]
  public void FindEndpointsWithoutPermission_ShouldListOnlyEndpointsThatNeedAPermissionAndCannotGetOne()
  {
    var sut = CreateSut(
      Endpoint("/pedidos", "POST", typeof(MiApi.Features.Pedidos.Commands.CrearPedido.CrearPedidoEndpoint)),
      Endpoint("/legacy/pedidos", "GET", typeof(MiApi.Pedidos.Endpoints.PedidosSinFeature)),
      Endpoint("/publico", "GET", typeof(MiApi.Pedidos.Endpoints.PedidosSinFeature), metadata: new PermissionsNotRequiredAttribute("público")),
      Endpoint("/diagnostico", "GET", typeof(MiApi.Pedidos.Endpoints.PedidosSinFeature), metadata: new RequiresDevelopmentAccessAttribute("dev")));

    var result = sut.FindEndpointsWithoutPermission();

    result.Should().ContainSingle()
      .Which.Should().Be("GET /legacy/pedidos (MiApi.Pedidos.Endpoints)");
  }

  private static EndpointDiscoveryService CreateSut(params Endpoint[] endpoints)
  {
    var serviceProvider = Substitute.For<IServiceProvider>();
    serviceProvider.GetService(typeof(EndpointDataSource)).Returns(new TestEndpointDataSource(endpoints));
    return new EndpointDiscoveryService(serviceProvider, new PermissionNamingService());
  }

  private static RouteEndpoint Endpoint(string pattern, string httpMethod, Type declaringType, string handlerName = "Handle", object? metadata = null)
  {
    var builder = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse(pattern), order: 0);
    builder.Metadata.Add(declaringType.GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic)!);
    builder.Metadata.Add(new HttpMethodMetadata([httpMethod]));
    if (metadata is not null)
    {
      builder.Metadata.Add(metadata);
    }

    return (RouteEndpoint)builder.Build();
  }

  private sealed class TestEndpointDataSource(params Endpoint[] endpoints) : EndpointDataSource
  {
    public override IReadOnlyList<Endpoint> Endpoints { get; } = endpoints;

    public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
  }
}
