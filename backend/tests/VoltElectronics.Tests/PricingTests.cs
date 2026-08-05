using VoltElectronics.Application.Common;

namespace VoltElectronics.Tests;

public class PricingTests
{
    [Fact]
    public void Totals_computes_flat_shipping_and_tax()
    {
        var (subtotal, shipping, tax, total) = Pricing.Totals(100m);

        Assert.Equal(100m, subtotal);
        Assert.Equal(24m, shipping);
        Assert.Equal(8.75m, tax);
        Assert.Equal(132.75m, total);
    }

    [Fact]
    public void Totals_rounds_tax_to_cents()
    {
        var (_, _, tax, _) = Pricing.Totals(249m);

        // 249 * 0.0875 = 21.7875 → 21.79
        Assert.Equal(21.79m, tax);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Totals_is_all_zero_for_empty_or_invalid_subtotal(decimal subtotal)
    {
        var totals = Pricing.Totals(subtotal);

        Assert.Equal((0m, 0m, 0m, 0m), totals);
    }

    [Fact]
    public void TaxFor_matches_mockup_rate()
    {
        Assert.Equal(Math.Round(1499m * 0.0875m, 2), Pricing.TaxFor(1499m));
    }
}
