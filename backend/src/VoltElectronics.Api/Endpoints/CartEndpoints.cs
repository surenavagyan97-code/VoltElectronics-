using VoltElectronics.Application.Carts;
using VoltElectronics.Application.Carts.Commands;
using VoltElectronics.Application.Carts.Queries;
using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Api.Endpoints;

public static class CartEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var cart = app.MapGroup("/api/cart").WithTags("Cart");

        cart.MapGet("/", (HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            WithCart(ctx, async key => Results.Ok(await dispatcher.Query(new GetCartQuery(key), ct))));

        cart.MapPost("/items", (AddCartItemRequest request, HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            WithCart(ctx, async key => ApiResults.Ok(
                await dispatcher.Send(new AddCartItemCommand(key, request.ProductId, request.Qty), ct))));

        cart.MapPut("/items/{productId:int}", (int productId, UpdateCartItemRequest request, HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            WithCart(ctx, async key => ApiResults.Ok(
                await dispatcher.Send(new UpdateCartItemQtyCommand(key, productId, request.Qty), ct))));

        cart.MapDelete("/items/{productId:int}", (int productId, HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            WithCart(ctx, async key => ApiResults.Ok(
                await dispatcher.Send(new RemoveCartItemCommand(key, productId), ct))));

        cart.MapDelete("/", (HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            WithCart(ctx, async key => ApiResults.Ok(
                await dispatcher.Send(new ClearCartCommand(key), ct))));

        cart.MapPut("/currency", (SetCartCurrencyRequest request, HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            WithCart(ctx, async key => ApiResults.Ok(
                await dispatcher.Send(new SetCartCurrencyCommand(key, request.Currency), ct))));

        // Fold the pre-login guest cart into the authenticated user's cart.
        cart.MapPost("/merge", async (HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            {
                var userId = ctx.User.GetUserId()!;
                return Guid.TryParse(ctx.Request.Headers[RequestIdentity.CartHeader], out var guestId)
                    ? ApiResults.Ok(await dispatcher.Send(new MergeGuestCartCommand(guestId, userId), ct))
                    : Results.BadRequest(new { error = $"Missing {RequestIdentity.CartHeader} header." });
            })
            .RequireAuthorization();
    }

    private static async Task<IResult> WithCart(HttpContext ctx, Func<CartKey, Task<IResult>> action)
    {
        var key = ctx.GetCartKey();
        if (!key.IsValid)
            return Results.BadRequest(new { error = $"Provide a {RequestIdentity.CartHeader} header or authenticate." });
        return await action(key);
    }
}
