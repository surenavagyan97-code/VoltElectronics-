using VoltElectronics.Domain.Carts;

namespace VoltElectronics.Application.Carts;

/// <summary>
/// Bridges <see cref="CartKey"/> — an application-layer notion of "who is asking" — onto the two
/// lookups the domain repository exposes. Kept here so every cart handler resolves a key the same way.
/// </summary>
internal static class CartLookup
{
    public static Task<Cart?> FindAsync(this ICartRepository carts, CartKey key, CancellationToken ct) =>
        key.UserId is not null
            ? carts.GetByUserIdAsync(key.UserId, ct)
            : carts.GetGuestCartAsync(key.GuestId!.Value, ct);

    /// <summary>Materializes the cart a key points at, registering it with the repository.</summary>
    public static Cart Start(this ICartRepository carts, CartKey key)
    {
        var cart = key.UserId is not null ? Cart.ForUser(key.UserId) : Cart.ForGuest(key.GuestId!.Value);
        carts.Add(cart);
        return cart;
    }
}
