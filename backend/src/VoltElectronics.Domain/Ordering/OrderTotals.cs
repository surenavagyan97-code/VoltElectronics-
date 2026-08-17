namespace VoltElectronics.Domain.Ordering;

/// <summary>
/// Value object: the money actually charged, in the currency the order was settled in. This is
/// the gateway's settlement currency (AMD), not necessarily the currency the shopper was
/// browsing/checking out in — the storefront can display USD/EUR for convenience, but the charge
/// itself always goes through in AMD. <paramref name="ExchangeRate"/> is kept so historical
/// revenue can be normalized back to the store's base currency long after the rates have moved.
/// </summary>
public sealed record OrderTotals(
    decimal Subtotal,
    decimal Shipping,
    decimal Tax,
    decimal Total,
    string Currency,
    decimal ExchangeRate);
