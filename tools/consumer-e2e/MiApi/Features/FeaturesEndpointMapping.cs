using MiApi.Features.Pedidos.Commands.CrearPedido;
using MiApi.Features.Pedidos.Queries.GetPedidos;
using MiApi.Legacy;

namespace MiApi.Features;

public static class FeaturesEndpointMapping
{
    public static void MapMiApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrearPedidoEndpoint();
        app.MapGetPedidosEndpoint();
        app.MapLegacyEndpoints();
    }
}
