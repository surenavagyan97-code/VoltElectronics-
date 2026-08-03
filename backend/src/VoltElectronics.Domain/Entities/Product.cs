using VoltElectronics.Domain.Enums;

namespace VoltElectronics.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Sku { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int Stock { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public string? Badge { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductSpec> Specs { get; set; } = new List<ProductSpec>();
}
