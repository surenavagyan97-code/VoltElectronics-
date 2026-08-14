namespace VoltElectronics.Application.Catalog;

public record CategoryDto(int Id, string Name, string Slug, int ProductCount, string? ImageUrl);

public record ProductListItemDto(
    int Id, string Name, string Slug, string Category, int CategoryId,
    decimal Price, decimal? CompareAtPrice, string? Badge,
    double Rating, int ReviewCount, int Stock, string? ImageUrl);

public record ProductSpecDto(string Name, string Value);

/// <summary>
/// One language's display texts; each field falls back to the product's canonical value
/// when null or missing.
/// </summary>
public record ProductTranslationDto(string Lang, string? Name, string? Description = null);

public record ProductImageDto(int Id, string Url, string ThumbUrl, string CardUrl, int SortOrder);

public record ProductDetailDto(
    int Id, string Name, string Slug, string Sku, string Category, int CategoryId,
    decimal Price, decimal? CompareAtPrice, string? Badge,
    double Rating, int ReviewCount, int Stock, string Description,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductSpecDto> Specs,
    IReadOnlyList<ProductListItemDto> Related);
