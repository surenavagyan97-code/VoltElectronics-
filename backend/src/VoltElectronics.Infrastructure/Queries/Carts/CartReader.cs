using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Carts;
using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Domain.Ordering;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Queries.Carts;

/// <summary>
/// The shopper-facing cart projection: cart lines joined back to live product data (name, image,
/// stock, current price) with every amount converted into the cart's display currency.
/// </summary>
internal sealed class CartReader(AppDbContext db, ICurrencyConverter currency) : ICartReader
{
    public async Task<CartDto> ReadAsync(CartKey key, CancellationToken cancellationToken = default)
    {
        var carts = db.Carts.AsNoTracking().Include(c => c.Items);
        var cart = key.UserId is not null
            ? await carts.FirstOrDefaultAsync(c => c.UserId == key.UserId, cancellationToken)
            : await carts.FirstOrDefaultAsync(c => c.Id == key.GuestId && c.UserId == null, cancellationToken);

        var cur = cart?.Currency ?? currency.BaseCurrency;
        if (cart is null || cart.Items.Count == 0)
            return new CartDto(cart?.Id ?? Guid.Empty, [], 0, 0, 0, 0, 0, cur);

        var ids = cart.Items.Select(i => i.ProductId).ToArray();
        var products = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new
            {
                p.Id, p.Name, p.Slug, Category = p.Category.Name, p.Price, p.Stock,
                ImageUrl = p.Images.OrderBy(i => i.SortOrder).Select(i => (string?)i.CardUrl).FirstOrDefault()
            })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = cart.Items
            .OrderBy(i => i.Id)
            .Select(i => (Line: i, Product: products[i.ProductId]))
            .Select(x => new CartItemDto(
                x.Product.Id, x.Product.Name, x.Product.Slug, x.Product.Category,
                currency.Convert(x.Product.Price, cur), x.Line.Qty,
                currency.Convert(x.Product.Price * x.Line.Qty, cur),
                x.Product.Stock, x.Product.ImageUrl))
            .ToList();

        // Compute shipping/tax on the true base-currency subtotal, then convert the totals together —
        // converting each line first and re-summing could drift a cent from per-line rounding.
        var (subtotal, shipping, tax, total) = PricingPolicy.Totals(
            cart.Items.Sum(i => products[i.ProductId].Price * i.Qty));

        return new CartDto(cart.Id, items, items.Sum(i => i.Qty),
            currency.Convert(subtotal, cur), currency.Convert(shipping, cur),
            currency.Convert(tax, cur), currency.Convert(total, cur), cur);
    }
}
