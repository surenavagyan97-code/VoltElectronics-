namespace VoltElectronics.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Snapshot at purchase time — survives later product edits/deletes.
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Qty { get; set; }
}
