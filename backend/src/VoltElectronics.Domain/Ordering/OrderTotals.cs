namespace VoltElectronics.Domain.Ordering;

/// <summary>
/// Value object: the money actually charged, in the currency the shopper checked out with.
/// <paramref name="ExchangeRate"/> is kept so historical revenue can be normalized back to the
/// store's base currency long after the rates have moved.
/// </summary>
public sealed record OrderTotals(
    decimal Subtotal,
    decimal Shipping,
    decimal Tax,
    decimal Total,
    string Currency,
    decimal ExchangeRate);
