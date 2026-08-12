namespace VoltElectronics.Application.Carts;

/// <summary>
/// Builds the shopper-facing cart projection. Cart mutations answer with the whole repriced cart,
/// and that view needs product names, images and converted prices — data the Cart aggregate
/// deliberately doesn't hold. Rather than let a command handler reach across aggregates, it asks
/// this read port, which the persistence layer implements alongside the other projections.
/// </summary>
public interface ICartReader
{
    /// <summary>An empty cart for a key that has none yet — never null.</summary>
    Task<CartDto> ReadAsync(CartKey key, CancellationToken cancellationToken = default);
}
