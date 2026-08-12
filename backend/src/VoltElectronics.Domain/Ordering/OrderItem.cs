namespace VoltElectronics.Domain.Ordering;

/// <summary>
/// Part of the <see cref="Order"/> aggregate. Name and price are snapshots taken at purchase
/// time so the line survives later product edits or deletions.
/// </summary>
public sealed class OrderItem
{
    private OrderItem() { }

    internal OrderItem(int productId, string productName, decimal unitPrice, int qty)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Qty = qty;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Qty { get; private set; }
}
