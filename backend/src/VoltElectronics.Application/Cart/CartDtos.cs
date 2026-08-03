namespace VoltElectronics.Application.Cart;

public record CartItemDto(
    int ProductId, string Name, string Slug, string Category,
    decimal Price, int Qty, decimal LineTotal, int Stock, string? ImageUrl);

public record CartDto(
    Guid Id, IReadOnlyList<CartItemDto> Items, int Count,
    decimal Subtotal, decimal Shipping, decimal Tax, decimal Total);

public record AddCartItemRequest(int ProductId, int Qty);
public record UpdateCartItemRequest(int Qty);

/// <summary>Identifies whose cart to operate on: an authenticated user's, or a guest cart by client GUID.</summary>
public record CartKey(string? UserId, Guid? GuestId)
{
    public bool IsValid => UserId is not null || GuestId is not null;
}
