// -----------------------------------------------------------------------
// <copyright file="ConsumerEndpointFixtures.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

// Clases con namespaces de una app que CONSUME el paquete (no de la librería): el descubrimiento
// toma el namespace real de MethodInfo.DeclaringType, así que cada handler vive en el namespace que
// se quiere probar. Ver EndpointDiscoveryConsumerNamespaceTests.

#pragma warning disable SA1403 // File may only contain a single namespace: a propósito, un namespace por caso.
#pragma warning disable SA1649 // File name should match first type name: archivo de fixtures, no de un tipo.

namespace MiApi.Features.Pedidos.Commands.CrearPedido
{
  internal static class CrearPedidoEndpoint
  {
    internal static void Handle()
    {
    }

    internal static void HandleOtraRuta()
    {
    }
  }
}

namespace MiApi.Features.Users.Commands.CreateUser
{
  internal static class CreateUserEndpoint
  {
    internal static void Handle()
    {
    }
  }
}

namespace OtraApi.Modulo.Features.Users.Commands.CreateUser
{
  internal static class CreateUserEndpoint
  {
    internal static void Handle()
    {
    }
  }
}

namespace MiApi.Pedidos.Endpoints
{
  internal static class PedidosSinFeature
  {
    internal static void Handle()
    {
    }
  }
}

#pragma warning restore SA1649
#pragma warning restore SA1403
