using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Ordering.Events;

/// <summary>
/// Raised once, when a gateway callback confirms payment. Reserving stock and emptying the
/// shopper's cart are consequences of that fact, not part of the Order aggregate's own invariants.
/// </summary>
public sealed record OrderPaidDomainEvent(
    int OrderId,
    string OrderNumber,
    Guid? CartId,
    IReadOnlyList<PurchasedLine> Lines) : IDomainEvent;

public sealed record PurchasedLine(int ProductId, int Qty);
