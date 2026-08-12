namespace VoltElectronics.Domain.Carts;

/// <summary>Part of the <see cref="Cart"/> aggregate — only ever created through the root.</summary>
public sealed class CartItem
{
    private CartItem() { }

    internal CartItem(int productId, int qty)
    {
        ProductId = productId;
        Qty = qty;
    }

    public int Id { get; private set; }
    public Guid CartId { get; private set; }
    /// <summary>Reference to the Product aggregate by identity — the cart never reaches into it.</summary>
    public int ProductId { get; private set; }
    public int Qty { get; private set; }

    internal void SetQty(int qty) => Qty = qty;
}
