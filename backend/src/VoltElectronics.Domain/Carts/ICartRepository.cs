namespace VoltElectronics.Domain.Carts;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Cart?> GetGuestCartAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken = default);
    void Add(Cart cart);
    void Remove(Cart cart);

    /// <summary>
    /// Drops every reference to a product from every cart. Deleting a product outright would
    /// otherwise be blocked by carts still holding it, and no single cart aggregate is in a
    /// position to know that — so this is expressed as a set operation on the collection.
    /// </summary>
    Task RemoveProductLinesAsync(int productId, CancellationToken cancellationToken = default);
}
