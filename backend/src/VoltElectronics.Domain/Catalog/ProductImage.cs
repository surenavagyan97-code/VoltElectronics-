namespace VoltElectronics.Domain.Catalog;

/// <summary>Part of the <see cref="Product"/> aggregate — only ever created through the root.</summary>
public sealed class ProductImage
{
    private ProductImage() { }

    internal ProductImage(string url, string thumbUrl, string cardUrl, int sortOrder)
    {
        Url = url;
        ThumbUrl = thumbUrl;
        CardUrl = cardUrl;
        SortOrder = sortOrder;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    /// <summary>Largest variant — product detail main viewer.</summary>
    public string Url { get; private set; } = null!;
    /// <summary>Small variant — admin thumbnails, detail-page thumbnail strip.</summary>
    public string ThumbUrl { get; private set; } = "";
    /// <summary>Medium variant — listing/featured cards, cart and order line items.</summary>
    public string CardUrl { get; private set; } = "";
    public int SortOrder { get; private set; }
}
