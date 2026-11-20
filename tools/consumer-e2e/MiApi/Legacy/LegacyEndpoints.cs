namespace MiApi.Legacy;

public static class LegacyEndpoints
{
    public static void MapLegacyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/legacy/reporte", () => Results.Ok("reporte"));
    }
}
