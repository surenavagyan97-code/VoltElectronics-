namespace VoltElectronics.Application.Delivery;

// What a delivery person sees about an assigned order: where to go, whom to hand it to,
// what it costs — but never the customer's account or payment internals.

public record DeliveryOrderItemDto(string ProductName, int Qty);

public record DeliveryOrderDto(
    string OrderNumber, string Status, DateTime CreatedAt,
    string FullName, string? Phone,
    string Street, string City, string State, string Zip,
    decimal Total, string Currency,
    IReadOnlyList<DeliveryOrderItemDto> Items);

public record UpdateDeliveryStatusRequest(string Status);
