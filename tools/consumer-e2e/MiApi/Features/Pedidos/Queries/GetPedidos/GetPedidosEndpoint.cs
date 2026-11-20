namespace MiApi.Features.Pedidos.Queries.GetPedidos;

public static class GetPedidosEndpoint
{
    public static void MapGetPedidosEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/pedidos", () => Results.Ok(new[] { "pedido-1" }))
           .WithSummary("Lista pedidos.");
    }
}
