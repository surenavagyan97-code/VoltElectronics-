namespace VoltElectronics.Domain.Ordering;

public interface IOrderRepository
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default);
    void Add(Order order);
}
