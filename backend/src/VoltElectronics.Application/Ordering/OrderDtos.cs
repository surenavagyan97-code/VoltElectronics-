namespace VoltElectronics.Application.Ordering;

/// <summary>Checkout body — email plus the address the order ships to.</summary>
public record CheckoutRequest(
    string Email,
    string FullName,
    string? Company,
    string Street,
    string City,
    string State,
    string Zip,
    string Phone);

/// <summary>Send the shopper to PaymentUrl; the gateway redirects back and the order flips to Processing.</summary>
public record CheckoutResponse(string OrderNumber, string PaymentUrl);

public record OrderItemDto(int ProductId, string ProductName, string? Slug, string? ImageUrl, decimal UnitPrice, int Qty);

public record OrderSummaryDto(
    string OrderNumber, string Status, DateTime CreatedAt, decimal Total, int ItemCount, string Currency);

public record OrderDetailDto(
    string OrderNumber, string Status, DateTime CreatedAt, DateTime? PaidAt,
    string? PaymentFailureReason,
    string ShipFullName, string? ShipCompany, string ShipStreet, string ShipCity,
    string ShipState, string ShipZip, string? ShipPhone,
    decimal Subtotal, decimal ShippingCost, decimal Tax, decimal Total, string Currency,
    IReadOnlyList<OrderItemDto> Items);

/// <summary>Outcome of a gateway callback — tells the API where to send the shopper.</summary>
public record PaymentCallbackOutcome(string? OrderNumber, bool Paid);
