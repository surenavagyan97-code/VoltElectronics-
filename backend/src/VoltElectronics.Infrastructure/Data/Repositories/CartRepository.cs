using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Carts;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class CartRepository(AppDbContext db) : ICartRepository
{
    public Task<Cart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task<Cart?> GetGuestCartAsync(Guid cartId, CancellationToken cancellationToken = default) =>
        db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId && c.UserId == null, cancellationToken);

    public Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken = default) =>
        db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);

    public void Add(Cart cart) => db.Carts.Add(cart);

    public void Remove(Cart cart) => db.Carts.Remove(cart);

    public async Task RemoveProductLinesAsync(int productId, CancellationToken cancellationToken = default)
    {
        // Tracked removal rather than ExecuteDelete so it commits with the unit of work.
        var lines = await db.Set<CartItem>()
            .Where(i => i.ProductId == productId)
            .ToListAsync(cancellationToken);
        db.RemoveRange(lines);
    }
}
