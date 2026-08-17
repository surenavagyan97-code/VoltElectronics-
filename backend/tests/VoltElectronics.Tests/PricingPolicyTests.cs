using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Tests;

public class PricingPolicyTests
{
    [Fact]
    public void Totals_computes_flat_shipping_and_tax()
    {
        var (subtotal, discount, shipping, tax, total) = PricingPolicy.Totals(100m);

        Assert.Equal(100m, subtotal);
        Assert.Equal(0m, discount);
        Assert.Equal(24m, shipping);
        Assert.Equal(8.75m, tax);
        Assert.Equal(132.75m, total);
    }

    [Fact]
    public void Totals_rounds_tax_to_cents()
    {
        var (_, _, _, tax, _) = PricingPolicy.Totals(249m);

        // 249 * 0.0875 = 21.7875 → 21.79
        Assert.Equal(21.79m, tax);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Totals_is_all_zero_for_empty_or_invalid_subtotal(decimal subtotal)
    {
        var totals = PricingPolicy.Totals(subtotal);

        Assert.Equal((0m, 0m, 0m, 0m, 0m), totals);
    }

    [Fact]
    public void TaxFor_matches_mockup_rate()
    {
        Assert.Equal(Math.Round(1499m * 0.0875m, 2), PricingPolicy.TaxFor(1499m));
    }

    [Fact]
    public void Totals_applies_discount_before_tax()
    {
        // $100 subtotal, $20 discount → taxed on $80, not $100.
        var (subtotal, discount, shipping, tax, total) = PricingPolicy.Totals(100m, 20m);

        Assert.Equal(100m, subtotal);
        Assert.Equal(20m, discount);
        Assert.Equal(24m, shipping);
        Assert.Equal(7m, tax); // 80 * 0.0875 = 7.00
        Assert.Equal(111m, total); // 80 + 24 + 7
    }

    [Fact]
    public void Totals_clamps_discount_to_the_subtotal()
    {
        var (_, discount, _, _, total) = PricingPolicy.Totals(50m, 999m);

        Assert.Equal(50m, discount);
        Assert.Equal(24m, total); // fully discounted subtotal, tax on $0, still pays shipping
    }
}
