using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Promotions;

namespace VoltElectronics.Domain.Carts;

/// <summary>
/// A shopper's basket. Quantities are validated against stock the caller supplies — the cart
/// holds product identities only, never a reference into the Product aggregate.
/// </summary>
public sealed class Cart : AggregateRoot
{
    private readonly List<CartItem> _items = [];

    private Cart() { }

    public Guid Id { get; private set; }
    public string? UserId { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? CouponCode { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<CartItem> Items => _items;

    public static Cart ForUser(string userId) => new() { Id = Guid.NewGuid(), UserId = userId };

    /// <summary>A guest's cart id is chosen client-side and travels in the X-Cart-Id header.</summary>
    public static Cart ForGuest(Guid cartId) => new() { Id = cartId };

    public void AddItem(int productId, int qty, int availableStock)
    {
        if (qty < 1) throw new DomainException("Quantity must be at least 1.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        var newQty = (item?.Qty ?? 0) + qty;
        if (newQty > availableStock) throw new DomainException($"Only {availableStock} in stock.");

        if (item is null) _items.Add(new CartItem(productId, qty));
        else item.SetQty(newQty);
        Touch();
    }

    /// <summary>Setting a quantity below 1 drops the line, matching the storefront's stepper.</summary>
    public void SetItemQty(int productId, int qty, int availableStock)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new DomainException("Item not in cart.");

        if (qty < 1)
        {
            _items.Remove(item);
        }
        else
        {
            if (qty > availableStock) throw new DomainException($"Only {availableStock} in stock.");
            item.SetQty(qty);
        }
        Touch();
    }

    public void RemoveItem(int productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;
        _items.Remove(item);
        Touch();
    }

    public void Clear()
    {
        if (_items.Count == 0 && CouponCode is null) return;
        _items.Clear();
        CouponCode = null;
        Touch();
    }

    public void ChangeCurrency(string currency)
    {
        Currency = currency.ToUpperInvariant();
        Touch();
    }

    public void ApplyCoupon(string code)
    {
        CouponCode = Promotion.Normalize(code);
        Touch();
    }

    public void RemoveCoupon()
    {
        CouponCode = null;
        Touch();
    }

    /// <summary>Cheapest merge path: an unclaimed guest cart simply becomes the user's cart.</summary>
    public void AssignTo(string userId)
    {
        UserId = userId;
        Touch();
    }

    /// <summary>Folds another cart's lines into this one, clamping combined quantities to stock.</summary>
    public void AbsorbFrom(Cart other, IReadOnlyDictionary<int, int> stockByProduct)
    {
        foreach (var incoming in other.Items)
        {
            var stock = stockByProduct.GetValueOrDefault(incoming.ProductId, incoming.Qty);
            var existing = _items.FirstOrDefault(i => i.ProductId == incoming.ProductId);
            if (existing is null) _items.Add(new CartItem(incoming.ProductId, Math.Min(incoming.Qty, stock)));
            else existing.SetQty(Math.Min(existing.Qty + incoming.Qty, stock));
        }
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
