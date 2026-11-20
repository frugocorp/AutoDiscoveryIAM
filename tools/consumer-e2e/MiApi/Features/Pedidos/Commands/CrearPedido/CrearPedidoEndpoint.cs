namespace MiApi.Features.Pedidos.Commands.CrearPedido;

public static class CrearPedidoEndpoint
{
    public static void MapCrearPedidoEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/pedidos", () => Results.Ok(new { creado = true }))
           .WithSummary("Crea un pedido.");
        // Segunda ruta del MISMO slice: comparte el permiso pedidos.crear_pedido.
        app.MapPost("/pedidos/borrador", () => Results.Ok(new { borrador = true }))
           .WithSummary("Guarda un pedido como borrador.");
    }
}
