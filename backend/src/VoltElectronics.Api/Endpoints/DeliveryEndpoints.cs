using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Delivery;
using VoltElectronics.Application.Delivery.Queries;
using VoltElectronics.Application.Identity;

namespace VoltElectronics.Api.Endpoints;

/// <summary>The delivery person's view: their own assignments, nothing else.</summary>
public static class DeliveryEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var delivery = app.MapGroup("/api/delivery")
            .WithTags("Delivery")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Courier));

        delivery.MapGet("/orders", async (
                HttpContext ctx, IDispatcher dispatcher, CancellationToken ct, string? status = null) =>
            Results.Ok(await dispatcher.Query(new GetDeliveryOrdersQuery(ctx.User.GetUserId()!, status), ct)));

        delivery.MapPut("/orders/{orderNumber}/status", async (
                string orderNumber, UpdateDeliveryStatusRequest request,
                HttpContext ctx, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(
                new UpdateDeliveryOrderStatusCommand(orderNumber, ctx.User.GetUserId()!, request.Status), ct)));
    }
}
