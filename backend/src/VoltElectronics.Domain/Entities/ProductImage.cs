namespace VoltElectronics.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required string Url { get; set; }
    public int SortOrder { get; set; }
}
