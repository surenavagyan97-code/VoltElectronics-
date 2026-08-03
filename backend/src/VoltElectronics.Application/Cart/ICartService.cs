namespace VoltElectronics.Application.Cart;

public interface ICartService
{
    Task<CartDto> GetAsync(CartKey key);
    Task<CartDto> AddItemAsync(CartKey key, int productId, int qty);
    Task<CartDto> UpdateItemAsync(CartKey key, int productId, int qty);
    Task<CartDto> RemoveItemAsync(CartKey key, int productId);
    Task<CartDto> ClearAsync(CartKey key);
    /// <summary>Merge a guest cart into the authenticated user's cart (called after login).</summary>
    Task<CartDto> MergeAsync(Guid guestCartId, string userId);
}
