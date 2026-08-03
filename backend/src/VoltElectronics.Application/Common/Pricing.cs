namespace VoltElectronics.Application.Common;

/// <summary>Single source of truth for order money math (mirrors the mockup: flat $24 shipping, 8.75% tax).</summary>
public static class Pricing
{
    public const decimal FlatShipping = 24m;
    public const decimal TaxRate = 0.0875m;

    public static decimal TaxFor(decimal subtotal) => Math.Round(subtotal * TaxRate, 2);

    public static (decimal Subtotal, decimal Shipping, decimal Tax, decimal Total) Totals(decimal subtotal)
    {
        if (subtotal <= 0) return (0, 0, 0, 0);
        var shipping = FlatShipping;
        var tax = TaxFor(subtotal);
        return (subtotal, shipping, tax, subtotal + shipping + tax);
    }
}
