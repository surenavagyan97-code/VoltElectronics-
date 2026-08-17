using VoltElectronics.Application.Catalog;
using VoltElectronics.Domain.Promotions;

namespace VoltElectronics.Infrastructure.Queries.Catalog;

/// <summary>
/// Applies automatic (no-code) Category/Product-scoped promotions — "sales" — to the price shown
/// on product cards/detail pages, the same way a manually-set CompareAtPrice would. This runs as
/// a post-fetch overlay in C# rather than inside the SQL projection: with only a handful of active
/// promotions at a time, that's far simpler than building a correlated-subquery expression tree,
/// at the cost of the shop page's price-band filter matching the pre-sale price. Order-scoped
/// promotions never affect a single product's displayed price, so they're not considered here.
/// </summary>
internal static class PromotionOverlay
{
    /// <summary>Only the item-scoped candidates matter for display — order-wide promotions/coupons
    /// only ever show up once the shopper has a cart, never on a product card.</summary>
    public static IReadOnlyList<Promotion> ItemScoped(IReadOnlyList<Promotion> automaticPromotions) =>
        automaticPromotions.Where(p => p.Scope != PromotionScope.Order).ToList();

    public static List<ProductListItemDto> Apply(List<ProductListItemDto> items, IReadOnlyList<Promotion> itemPromotions)
    {
        if (itemPromotions.Count == 0) return items;
        return items.Select(p =>
        {
            var (price, compareAtPrice) = Overlay(p.Price, p.CompareAtPrice, p.CategoryId, p.Id, itemPromotions);
            return price == p.Price ? p : p with { Price = price, CompareAtPrice = compareAtPrice };
        }).ToList();
    }

    public static ProductDetailDto Apply(ProductDetailDto detail, IReadOnlyList<Promotion> itemPromotions)
    {
        if (itemPromotions.Count == 0) return detail;
        var (price, compareAtPrice) = Overlay(detail.Price, detail.CompareAtPrice, detail.CategoryId, detail.Id, itemPromotions);
        return price == detail.Price ? detail : detail with { Price = price, CompareAtPrice = compareAtPrice };
    }

    private static (decimal Price, decimal? CompareAtPrice) Overlay(
        decimal price, decimal? compareAtPrice, int categoryId, int productId, IReadOnlyList<Promotion> itemPromotions)
    {
        var best = itemPromotions
            .Where(p => Matches(p, categoryId, productId))
            .Select(p => p.ComputeDiscount(price))
            .DefaultIfEmpty(0m)
            .Max();
        if (best <= 0) return (price, compareAtPrice);

        // Show the highest available "was" price crossed out — the product's own markdown if it
        // has one and it's higher than the sale price, otherwise the pre-sale price itself.
        var reference = compareAtPrice is > 0 ? compareAtPrice.Value : price;
        var effective = price - best;
        return (effective, reference > effective ? reference : null);
    }

    private static bool Matches(Promotion promotion, int categoryId, int productId) => promotion.Scope switch
    {
        PromotionScope.Category => promotion.CategoryId == categoryId,
        PromotionScope.Product => promotion.Products.Any(p => p.ProductId == productId),
        _ => false,
    };
}
