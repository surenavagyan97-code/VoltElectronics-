using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    // Items always ride along: MarkPaid snapshots them into its domain event.
    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public Task<Order?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default) =>
        db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PaymentId == paymentId, cancellationToken);

    public void Add(Order order) => db.Orders.Add(order);
}
