using VoltElectronics.Domain.Enums;

namespace VoltElectronics.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public required string OrderNumber { get; set; }
    public string? UserId { get; set; }
    public string? GuestEmail { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    public required string ShipFullName { get; set; }
    public string? ShipCompany { get; set; }
    public required string ShipStreet { get; set; }
    public required string ShipCity { get; set; }
    public required string ShipState { get; set; }
    public required string ShipZip { get; set; }
    public string? ShipPhone { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    // Cart that produced this order — cleared by the payment webhook on success.
    public Guid? CartId { get; set; }

    public string? StripePaymentIntentId { get; set; }
    public string? PaymentFailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
