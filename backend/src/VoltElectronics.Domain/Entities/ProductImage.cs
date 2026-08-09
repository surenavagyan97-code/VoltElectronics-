namespace VoltElectronics.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    /// <summary>Largest variant — product detail main viewer.</summary>
    public required string Url { get; set; }
    /// <summary>Small variant — admin thumbnails, detail-page thumbnail strip.</summary>
    public string ThumbUrl { get; set; } = "";
    /// <summary>Medium variant — listing/featured cards, cart and order line items.</summary>
    public string CardUrl { get; set; } = "";
    public int SortOrder { get; set; }
}
