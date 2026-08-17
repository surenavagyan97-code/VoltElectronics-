namespace VoltElectronics.Domain.Promotions;

/// <summary>
/// Single source of truth for turning a cart/order's lines into a discount, given whichever
/// promotions are in play. Pure function over already-fetched data — callers (CartReader,
/// CheckoutHandler) do the fetching via <see cref="IPromotionRepository"/>.
///
/// Rule: at most one promotion applies per line item (the best one wins, never stacked with
/// another item-level promotion), and at most one order-level promotion applies to the cart as a
/// whole (again the best one wins) — but an item-level result and an order-level result *do*
/// stack with each other, since they're different kinds of discount (a per-product sale, and a
/// cart-wide coupon on top of it).
/// </summary>
public static class PromotionPricing
{
    public sealed record Line(int ProductId, int CategoryId, decimal UnitPriceBase, int Qty);

    public sealed record Outcome(decimal ItemDiscount, decimal OrderDiscount, IReadOnlyList<int> AppliedPromotionIds)
    {
        public decimal Total => ItemDiscount + OrderDiscount;
    }

    /// <param name="automaticPromotions">Every currently-active, in-window, code-less promotion.</param>
    /// <param name="codedPromotion">The promotion the shopper's entered code resolved to, or null —
    /// caller is responsible for having already checked <see cref="Promotion.ValidateWindow"/> on it.</param>
    public static Outcome Compute(
        IReadOnlyList<Line> lines, decimal subtotalBase,
        IReadOnlyList<Promotion> automaticPromotions, Promotion? codedPromotion)
    {
        var appliedIds = new HashSet<int>();

        var itemCandidates = automaticPromotions.Where(p => p.Scope != PromotionScope.Order).ToList();
        if (codedPromotion is { Scope: not PromotionScope.Order }) itemCandidates.Add(codedPromotion);

        decimal itemDiscount = 0;
        foreach (var line in lines)
        {
            Promotion? best = null;
            var bestDiscount = 0m;
            foreach (var promotion in itemCandidates)
            {
                if (!Matches(promotion, line.CategoryId, line.ProductId)) continue;
                var discount = promotion.ComputeDiscount(line.UnitPriceBase) * line.Qty;
                if (discount > bestDiscount) { bestDiscount = discount; best = promotion; }
            }
            if (best is null) continue;
            itemDiscount += bestDiscount;
            appliedIds.Add(best.Id);
        }

        var orderCandidates = automaticPromotions.Where(p => p.Scope == PromotionScope.Order).ToList();
        if (codedPromotion is { Scope: PromotionScope.Order }) orderCandidates.Add(codedPromotion);

        Promotion? bestOrderPromotion = null;
        var bestOrderDiscount = 0m;
        var remainingSubtotal = Math.Max(0, subtotalBase - itemDiscount);
        foreach (var promotion in orderCandidates)
        {
            if (promotion.MinSubtotal is not null && subtotalBase < promotion.MinSubtotal) continue;
            var discount = promotion.ComputeDiscount(remainingSubtotal);
            if (discount > bestOrderDiscount) { bestOrderDiscount = discount; bestOrderPromotion = promotion; }
        }
        if (bestOrderPromotion is not null) appliedIds.Add(bestOrderPromotion.Id);

        return new Outcome(itemDiscount, bestOrderDiscount, appliedIds.ToList());
    }

    private static bool Matches(Promotion promotion, int categoryId, int productId) => promotion.Scope switch
    {
        PromotionScope.Category => promotion.CategoryId == categoryId,
        PromotionScope.Product => promotion.Products.Any(p => p.ProductId == productId),
        _ => false,
    };
}
