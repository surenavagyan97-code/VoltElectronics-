using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Orders;
using VoltElectronics.Application.Admin.Queries;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Identity;
using VoltElectronics.Application.Ordering.Queries;

namespace VoltElectronics.Api.Endpoints.Admin;

public static class AdminOrderEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/admin/orders")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        orders.MapGet("/", async (
                IDispatcher dispatcher, CancellationToken ct,
                int page = 1, int pageSize = 20, string? status = null, string? search = null) =>
            Results.Ok(await dispatcher.Query(new AdminGetOrdersQuery(page, pageSize, status, search), ct)));

        orders.MapGet("/stats", async (IDispatcher dispatcher, CancellationToken ct) =>
            Results.Ok(await dispatcher.Query(new AdminGetOrderStatsQuery(), ct)));

        // Admin bypasses the owner check by design — reuse the detail projection.
        orders.MapGet("/{orderNumber}", async (string orderNumber, IDispatcher dispatcher, CancellationToken ct) =>
            await dispatcher.Query(new GetOrderQuery(orderNumber, BypassOwnerCheck: true), ct) is { } order
                ? Results.Ok(order)
                : Results.NotFound());

        orders.MapPut("/{orderNumber}/status", async (
                string orderNumber, UpdateOrderStatusRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new UpdateOrderStatusCommand(orderNumber, request.Status), ct)));

        app.MapGet("/api/admin/analytics", async (IDispatcher dispatcher, CancellationToken ct) =>
                Results.Ok(await dispatcher.Query(new GetAnalyticsQuery(), ct)))
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
    }
}
