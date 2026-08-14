using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Delivery;
using VoltElectronics.Application.Delivery.Queries;
using VoltElectronics.Domain.Ordering;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Queries.Delivery;

internal sealed class GetDeliveryOrdersHandler(AppDbContext db)
    : IQueryHandler<GetDeliveryOrdersQuery, IReadOnlyList<DeliveryOrderDto>>
{
    public async Task<IReadOnlyList<DeliveryOrderDto>> HandleAsync(
        GetDeliveryOrdersQuery query, CancellationToken cancellationToken)
    {
        var q = db.Orders.AsNoTracking().Where(o => o.AssignedCourierId == query.CourierId);
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var parsed))
            q = q.Where(o => o.Status == parsed);

        return await q
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new DeliveryOrderDto(
                o.OrderNumber, o.Status.ToString(), o.CreatedAt,
                o.ShipTo.FullName, o.ShipTo.Phone,
                o.ShipTo.Street, o.ShipTo.City, o.ShipTo.State, o.ShipTo.Zip,
                o.Totals.Total, o.Totals.Currency,
                o.Items.Select(i => new DeliveryOrderItemDto(i.ProductName, i.Qty)).ToList()))
            .ToListAsync(cancellationToken);
    }
}
