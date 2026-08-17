namespace VoltElectronics.Domain.Ordering;

/// <summary>Single source of truth for order money math (mirrors the mockup: flat $24 shipping, 8.75% tax).</summary>
public static class PricingPolicy
{
    public const decimal FlatShipping = 24m;
    public const decimal TaxRate = 0.0875m;

    public static decimal TaxFor(decimal subtotal) => Math.Round(subtotal * TaxRate, 2);

    /// <summary>
    /// <paramref name="discount"/> (from <see cref="Promotions.PromotionPricing"/>) is subtracted
    /// from the subtotal before tax, so shoppers are taxed on what they actually pay — shipping is
    /// flat regardless of discount.
    /// </summary>
    public static (decimal Subtotal, decimal Discount, decimal Shipping, decimal Tax, decimal Total) Totals(
        decimal subtotal, decimal discount = 0)
    {
        if (subtotal <= 0) return (0, 0, 0, 0, 0);
        discount = Math.Clamp(discount, 0, subtotal);
        var discounted = subtotal - discount;
        var shipping = FlatShipping;
        var tax = TaxFor(discounted);
        return (subtotal, discount, shipping, tax, discounted + shipping + tax);
    }
}
