using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Ordering.Events;

namespace VoltElectronics.Application.Ordering.EventHandlers;

/// <summary>
/// Payment confirmed, so the goods are now committed: draw down stock and empty the basket that
/// produced the order. These touch other aggregates, which is exactly why they hang off an event
/// rather than sitting inside <c>Order.MarkPaid</c>.
///
/// No save here — the unit of work commits after dispatching, so this runs in the same transaction
/// as the order's own state change and can't half-apply.
/// </summary>
internal sealed class ReserveStockOnOrderPaid(
    IProductRepository products,
    ICartRepository carts) : IDomainEventHandler<OrderPaidDomainEvent>
{
    public async Task HandleAsync(OrderPaidDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var purchased = await products.GetByIdsAsync(
            domainEvent.Lines.Select(l => l.ProductId).ToArray(), cancellationToken);
        var byId = purchased.ToDictionary(p => p.Id);

        foreach (var line in domainEvent.Lines)
        {
            if (byId.TryGetValue(line.ProductId, out var product))
                product.ReduceStock(line.Qty);
        }

        if (domainEvent.CartId is not Guid cartId) return;

        var cart = await carts.GetByIdAsync(cartId, cancellationToken);
        cart?.Clear();
    }
}
