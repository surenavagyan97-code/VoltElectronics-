using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Catalog;

public sealed class Product : AggregateRoot
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductSpec> _specs = [];

    private Product() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public string Description { get; private set; } = "";
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public int Stock { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public string? Badge { get; private set; }
    public double Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<ProductImage> Images => _images;
    public IReadOnlyList<ProductSpec> Specs => _specs;

    public bool IsPurchasable => Status == ProductStatus.Active;

    /// <param name="slug">Uniqueness is a catalog-wide rule, so the caller resolves it against the repository.</param>
    public static Product Create(
        string name, string slug, string sku, int categoryId, string? description,
        decimal price, decimal? compareAtPrice, int stock, ProductStatus status, string? badge)
    {
        var product = new Product { Slug = slug };
        product.Describe(name, sku, categoryId, description, price, compareAtPrice, stock, status, badge);
        return product;
    }

    public void Describe(
        string name, string sku, int categoryId, string? description,
        decimal price, decimal? compareAtPrice, int stock, ProductStatus status, string? badge)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required.");
        if (price <= 0) throw new DomainException("Price must be positive.");
        if (stock < 0) throw new DomainException("Stock cannot be negative.");

        Name = name.Trim();
        Sku = sku.Trim();
        CategoryId = categoryId;
        Description = description ?? "";
        Price = price;
        CompareAtPrice = compareAtPrice;
        Stock = stock;
        Status = status;
        Badge = string.IsNullOrWhiteSpace(badge) ? null : badge.Trim();
    }

    /// <summary>The name drives the URL, so a rename needs a freshly resolved unique slug.</summary>
    public bool NeedsNewSlug(string newName) => !string.Equals(Name, newName.Trim(), StringComparison.Ordinal);

    public void ChangeSlug(string slug) => Slug = slug;

    public ProductImage AddImage(string url, string thumbUrl, string cardUrl)
    {
        var image = new ProductImage(url, thumbUrl, cardUrl,
            _images.Count == 0 ? 0 : _images.Max(i => i.SortOrder) + 1);
        _images.Add(image);
        return image;
    }

    public void RemoveImage(int imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new DomainException("Image not found.");
        _images.Remove(image);
    }

    public void ReplaceSpecs(IEnumerable<(string Name, string Value)> specs)
    {
        _specs.Clear();
        var sortOrder = 0;
        foreach (var (name, value) in specs)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) continue;
            _specs.Add(new ProductSpec(name.Trim(), value.Trim(), sortOrder++));
        }
    }

    /// <summary>Reviews aren't modeled yet — ratings enter through seeding and imports only.</summary>
    public void SetRating(double rating, int reviewCount)
    {
        if (rating is < 0 or > 5) throw new DomainException("Rating must be between 0 and 5.");
        if (reviewCount < 0) throw new DomainException("Review count cannot be negative.");
        Rating = rating;
        ReviewCount = reviewCount;
    }

    /// <summary>Retires the product without losing the order history that references it.</summary>
    public void Archive() => Status = ProductStatus.Archived;

    public void EnsureCanFulfil(int qty)
    {
        if (!IsPurchasable) throw new DomainException($"\"{Name}\" is no longer available.");
        if (qty > Stock) throw new DomainException($"Only {Stock} of \"{Name}\" left in stock.");
    }

    public void ReduceStock(int qty) => Stock = Math.Max(0, Stock - qty);
}
