using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Cart;
using VoltElectronics.Application.Common;
using VoltElectronics.Domain.Enums;
using VoltElectronics.Infrastructure.Data;
using CartEntity = VoltElectronics.Domain.Entities.Cart;
using CartItemEntity = VoltElectronics.Domain.Entities.CartItem;

namespace VoltElectronics.Infrastructure.Carts;

public class CartService(AppDbContext db) : ICartService
{
    public async Task<CartDto> GetAsync(CartKey key)
    {
        var cart = await FindAsync(key);
        return ToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(CartKey key, int productId, int qty)
    {
        if (qty < 1) throw new CartException("Quantity must be at least 1.");
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.Status == ProductStatus.Active)
            ?? throw new CartException("Product not found.");

        var cart = await FindAsync(key) ?? Create(key);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        var newQty = (item?.Qty ?? 0) + qty;
        if (newQty > product.Stock) throw new CartException($"Only {product.Stock} in stock.");

        if (item is null)
            cart.Items.Add(new CartItemEntity { ProductId = productId, Qty = qty });
        else
            item.Qty = newQty;

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(await FindAsync(key));
    }

    public async Task<CartDto> UpdateItemAsync(CartKey key, int productId, int qty)
    {
        var cart = await FindAsync(key) ?? throw new CartException("Cart not found.");
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new CartException("Item not in cart.");

        if (qty < 1)
        {
            db.CartItems.Remove(item);
            cart.Items.Remove(item);
        }
        else
        {
            if (qty > item.Product.Stock) throw new CartException($"Only {item.Product.Stock} in stock.");
            item.Qty = qty;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(CartKey key, int productId)
    {
        var cart = await FindAsync(key);
        var item = cart?.Items.FirstOrDefault(i => i.ProductId == productId);
        if (cart is not null && item is not null)
        {
            db.CartItems.Remove(item);
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return ToDto(cart);
    }

    public async Task<CartDto> ClearAsync(CartKey key)
    {
        var cart = await FindAsync(key);
        if (cart is not null && cart.Items.Count > 0)
        {
            db.CartItems.RemoveRange(cart.Items);
            cart.Items.Clear();
            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return ToDto(cart);
    }

    public async Task<CartDto> MergeAsync(Guid guestCartId, string userId)
    {
        var guestCart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == guestCartId && c.UserId == null);
        var userCart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (guestCart is null)
            return ToDto(userCart);

        if (userCart is null)
        {
            // Cheapest merge: the guest cart simply becomes the user's cart.
            guestCart.UserId = userId;
            guestCart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return ToDto(guestCart);
        }

        foreach (var guestItem in guestCart.Items)
        {
            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == guestItem.ProductId);
            if (existing is null)
                userCart.Items.Add(new CartItemEntity { ProductId = guestItem.ProductId, Qty = guestItem.Qty });
            else
                existing.Qty = Math.Min(existing.Qty + guestItem.Qty, guestItem.Product.Stock);
        }

        db.Carts.Remove(guestCart);
        userCart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ToDto(await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Include(c => c.Items).ThenInclude(i => i.Product.Category)
            .FirstAsync(c => c.Id == userCart.Id));
    }

    private Task<CartEntity?> FindAsync(CartKey key)
    {
        var q = db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .AsSplitQuery();
        return key.UserId is not null
            ? q.FirstOrDefaultAsync(c => c.UserId == key.UserId)
            : q.FirstOrDefaultAsync(c => c.Id == key.GuestId && c.UserId == null);
    }

    private CartEntity Create(CartKey key)
    {
        var cart = new CartEntity
        {
            // A guest's cart id is chosen client-side; a user's cart gets a fresh id.
            Id = key.UserId is null ? key.GuestId!.Value : Guid.NewGuid(),
            UserId = key.UserId
        };
        db.Carts.Add(cart);
        return cart;
    }

    private static CartDto ToDto(CartEntity? cart)
    {
        if (cart is null || cart.Items.Count == 0)
            return new CartDto(cart?.Id ?? Guid.Empty, [], 0, 0, 0, 0, 0);

        var items = cart.Items
            .OrderBy(i => i.Id)
            .Select(i => new CartItemDto(
                i.ProductId, i.Product.Name, i.Product.Slug, i.Product.Category.Name,
                i.Product.Price, i.Qty, i.Product.Price * i.Qty, i.Product.Stock,
                i.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault()))
            .ToList();

        var (subtotal, shipping, tax, total) = Pricing.Totals(items.Sum(i => i.LineTotal));
        return new CartDto(cart.Id, items, items.Sum(i => i.Qty), subtotal, shipping, tax, total);
    }
}

/// <summary>Business-rule violation surfaced to the API as a 400.</summary>
public class CartException(string message) : Exception(message);
