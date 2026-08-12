using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Ordering.Events;

namespace VoltElectronics.Domain.Ordering;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public int Id { get; private set; }
    public string OrderNumber { get; private set; } = null!;
    public string? UserId { get; private set; }
    public string? GuestEmail { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.PendingPayment;

    public ShippingAddress ShipTo { get; private set; } = null!;
    public OrderTotals Totals { get; private set; } = null!;

    /// <summary>Cart that produced this order — emptied once payment succeeds.</summary>
    public Guid? CartId { get; private set; }

    // Gateway-assigned payment id (e.g. Ameriabank vPOS PaymentID) + which provider issued it.
    public string? PaymentId { get; private set; }
    public string? PaymentProvider { get; private set; }
    public string? PaymentFailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items;

    public bool IsAwaitingPayment => Status == OrderStatus.PendingPayment;

    public static Order Place(
        string orderNumber,
        string? userId,
        string email,
        ShippingAddress shipTo,
        OrderTotals totals,
        Guid? cartId,
        string paymentProvider,
        IEnumerable<OrderLine> lines)
    {
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            GuestEmail = email.Trim(),
            ShipTo = shipTo,
            Totals = totals,
            CartId = cartId,
            PaymentProvider = paymentProvider
        };

        foreach (var line in lines)
            order._items.Add(new OrderItem(line.ProductId, line.ProductName, line.UnitPrice, line.Qty));

        if (order._items.Count == 0) throw new DomainException("Your cart is empty.");
        return order;
    }

    public void AttachPayment(string? paymentId) => PaymentId = paymentId;

    /// <summary>The gateway declined; the order stays payable so the shopper can retry.</summary>
    public void RecordPaymentFailure(string? reason) => PaymentFailureReason = reason;

    /// <summary>The gateway could not even be reached — nothing to retry against.</summary>
    public void AbandonUnpaid(string? reason)
    {
        Status = OrderStatus.Cancelled;
        PaymentFailureReason = reason;
    }

    public void MarkPaid(DateTime paidAtUtc)
    {
        if (!IsAwaitingPayment) throw new DomainException($"Order {OrderNumber} is no longer awaiting payment.");

        Status = OrderStatus.Processing;
        PaidAt = paidAtUtc;
        PaymentFailureReason = null;

        Raise(new OrderPaidDomainEvent(Id, OrderNumber, CartId,
            _items.Select(i => new PurchasedLine(i.ProductId, i.Qty)).ToList()));
    }

    public void ChangeStatus(OrderStatus status) => Status = status;

    /// <summary>
    /// Guest orders are readable by anyone who knows the order number *and* the checkout email;
    /// a user's own orders are readable by that user.
    /// </summary>
    public bool IsVisibleTo(string? userId, string? email) =>
        UserId is not null
            ? UserId == userId
            : email is not null && string.Equals(GuestEmail, email, StringComparison.OrdinalIgnoreCase);
}

public sealed record OrderLine(int ProductId, string ProductName, decimal UnitPrice, int Qty);
