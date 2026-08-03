namespace VoltElectronics.Domain.Entities;

public class ProductSpec
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required string Name { get; set; }
    public required string Value { get; set; }
    public int SortOrder { get; set; }
}
