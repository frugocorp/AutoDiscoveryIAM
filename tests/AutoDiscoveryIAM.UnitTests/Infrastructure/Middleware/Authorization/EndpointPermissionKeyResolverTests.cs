// -----------------------------------------------------------------------
// <copyright file="EndpointPermissionKeyResolverTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Middleware.Authorization;

/// <summary>
/// La clave se deriva del endpoint que el router ya eligió (usa los handlers de
/// <c>ConsumerEndpointFixtures.cs</c>, declarados en namespaces de una app consumidora).
/// </summary>
public class EndpointPermissionKeyResolverTests
{
  /// <summary>
  /// Antes, solo la primera ruta de un slice quedaba en la caché de rutas: las demás respondían 403
  /// a todo usuario sin flags especiales aunque tuviera el permiso.
  /// </summary>
  [Fact]
  public void ResolveKey_ForEveryRouteOfTheSameSlice_ShouldReturnTheSameKey()
  {
    var slice = typeof(MiApi.Features.Pedidos.Commands.CrearPedido.CrearPedidoEndpoint);
    var first = Endpoint("/pedidos", slice, "Handle");
    var second = Endpoint("/pedidos/borrador", slice, "HandleOtraRuta");
    var sut = CreateSut(first, second);

    sut.ResolveKey(first).Should().Be("pedidos.crear_pedido");
    sut.ResolveKey(second).Should().Be("pedidos.crear_pedido");
  }

  /// <summary>
  /// Dos namespaces con la misma clave: se niega el acceso a ambos (null → 403) en vez de dejar que
  /// uno quede autorizado por el permiso del otro.
  /// </summary>
  [Fact]
  public void ResolveKey_WhenTwoNamespacesProduceTheSameKey_ShouldReturnNullForBoth()
  {
    var mine = Endpoint("/users", typeof(MiApi.Features.Users.Commands.CreateUser.CreateUserEndpoint));
    var other = Endpoint("/modulo/users", typeof(OtraApi.Modulo.Features.Users.Commands.CreateUser.CreateUserEndpoint));
    var sut = CreateSut(mine, other);

    sut.ResolveKey(mine).Should().BeNull();
    sut.ResolveKey(other).Should().BeNull();
  }

  [Fact]
  public void ResolveKey_WhenNamespaceHasNoFeaturesSegment_ShouldReturnNull()
  {
    var endpoint = Endpoint("/legacy/pedidos", typeof(MiApi.Pedidos.Endpoints.PedidosSinFeature));
    var sut = CreateSut(endpoint);

    sut.ResolveKey(endpoint).Should().BeNull();
  }

  [Fact]
  public void ResolveKey_WhenEndpointHasNoHandlerMethod_ShouldReturnNull()
  {
    var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "sin handler");
    var sut = CreateSut();

    sut.ResolveKey(endpoint).Should().BeNull();
  }

  private static EndpointPermissionKeyResolver CreateSut(params Endpoint[] endpoints)
  {
    var services = new ServiceCollection();
    services.AddSingleton<EndpointDataSource>(new TestEndpointDataSource(endpoints));
    return new EndpointPermissionKeyResolver(services.BuildServiceProvider());
  }

  private static RouteEndpoint Endpoint(string pattern, Type declaringType, string handlerName = "Handle")
  {
    var builder = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse(pattern), order: 0);
    builder.Metadata.Add(declaringType.GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic)!);
    return (RouteEndpoint)builder.Build();
  }

  private sealed class TestEndpointDataSource(params Endpoint[] endpoints) : EndpointDataSource
  {
    public override IReadOnlyList<Endpoint> Endpoints { get; } = endpoints;

    public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
  }
}
