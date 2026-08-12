using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Ordering;
using VoltElectronics.Application.Ordering.Commands;
using VoltElectronics.Application.Ordering.Queries;

namespace VoltElectronics.Api.Endpoints;

public static class OrderEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders").WithTags("Orders");

        orders.MapGet("/", async (HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
                Results.Ok(await dispatcher.Query(new GetMyOrdersQuery(ctx.User.GetUserId()!), ct)))
            .RequireAuthorization();

        // Owner access; guests pass the email used at checkout as ?email=.
        orders.MapGet("/{orderNumber}", async (string orderNumber, HttpContext ctx, IDispatcher dispatcher, CancellationToken ct, string? email = null) =>
            await dispatcher.Query(new GetOrderQuery(orderNumber, ctx.User.GetUserId(), email), ct) is { } order
                ? Results.Ok(order)
                : Results.NotFound());

        app.MapPost("/api/checkout", async (CheckoutRequest request, HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            {
                var key = ctx.GetCartKey();
                if (!key.IsValid)
                    return Results.BadRequest(new { error = $"Provide a {RequestIdentity.CartHeader} header or authenticate." });

                return ApiResults.Ok(await dispatcher.Send(
                    new CheckoutCommand(key, ctx.User.GetUserId(), request), ct));
            })
            .WithTags("Orders");
    }
}
