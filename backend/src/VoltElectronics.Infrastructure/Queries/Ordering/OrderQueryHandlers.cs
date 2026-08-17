using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Ordering;
using VoltElectronics.Application.Ordering.Queries;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Queries.Ordering;

internal sealed class GetMyOrdersHandler(AppDbContext db)
    : IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    public async Task<IReadOnlyList<OrderSummaryDto>> HandleAsync(
        GetMyOrdersQuery query, CancellationToken cancellationToken) =>
        await db.Orders.AsNoTracking()
            .Where(o => o.UserId == query.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.OrderNumber, o.Status.ToString(), o.CreatedAt,
                o.Totals.Total, o.Items.Sum(i => i.Qty), o.Totals.Currency))
            .ToListAsync(cancellationToken);
}

internal sealed class GetOrderHandler(AppDbContext db) : IQueryHandler<GetOrderQuery, OrderDetailDto?>
{
    public async Task<OrderDetailDto?> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == query.OrderNumber, cancellationToken);
        if (order is null) return null;
        if (!query.BypassOwnerCheck && !order.IsVisibleTo(query.UserId, query.Email)) return null;

        // Slug and image come from the live product; both stay null for archived-then-removed ones.
        var ids = order.Items.Select(i => i.ProductId).ToArray();
        var products = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new
            {
                p.Id, p.Slug,
                ImageUrl = p.Images.OrderBy(i => i.SortOrder).Select(i => (string?)i.CardUrl).FirstOrDefault()
            })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new OrderDetailDto(
            order.OrderNumber, order.Status.ToString(), order.CreatedAt, order.PaidAt,
            order.PaymentFailureReason,
            order.ShipTo.FullName, order.ShipTo.Company, order.ShipTo.Street, order.ShipTo.City,
            order.ShipTo.State, order.ShipTo.Zip, order.ShipTo.Phone,
            order.Totals.Subtotal, order.Totals.Discount, order.Totals.Shipping, order.Totals.Tax, order.Totals.Total,
            order.Totals.Currency, order.CouponCode,
            order.Items.Select(i => new OrderItemDto(
                i.ProductId, i.ProductName,
                products.TryGetValue(i.ProductId, out var p) ? p.Slug : null,
                products.TryGetValue(i.ProductId, out var pi) ? pi.ImageUrl : null,
                i.UnitPrice, i.Qty)).ToList());
    }
}
