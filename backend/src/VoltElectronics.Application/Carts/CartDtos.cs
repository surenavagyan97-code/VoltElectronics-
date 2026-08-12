namespace VoltElectronics.Application.Carts;

public record CartItemDto(
    int ProductId, string Name, string Slug, string Category,
    decimal Price, int Qty, decimal LineTotal, int Stock, string? ImageUrl);

public record CartDto(
    Guid Id, IReadOnlyList<CartItemDto> Items, int Count,
    decimal Subtotal, decimal Shipping, decimal Tax, decimal Total, string Currency);

// Request bodies. The cart these apply to comes from the caller's identity or the X-Cart-Id
// header, not the body, so these can't simply be the commands themselves.
public record AddCartItemRequest(int ProductId, int Qty);
public record UpdateCartItemRequest(int Qty);
public record SetCartCurrencyRequest(string Currency);

/// <summary>Identifies whose cart to operate on: an authenticated user's, or a guest cart by client GUID.</summary>
public record CartKey(string? UserId, Guid? GuestId)
{
    public bool IsValid => UserId is not null || GuestId is not null;
}
