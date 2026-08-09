namespace VoltElectronics.Application.Catalog;

public record CategoryDto(int Id, string Name, string Slug, int ProductCount);

public record ProductListItemDto(
    int Id, string Name, string Slug, string Category, int CategoryId,
    decimal Price, decimal? CompareAtPrice, string? Badge,
    double Rating, int ReviewCount, int Stock, string? ImageUrl);

public record ProductSpecDto(string Name, string Value);

public record ProductImageDto(int Id, string Url, string ThumbUrl, string CardUrl, int SortOrder);

public record ProductDetailDto(
    int Id, string Name, string Slug, string Sku, string Category, int CategoryId,
    decimal Price, decimal? CompareAtPrice, string? Badge,
    double Rating, int ReviewCount, int Stock, string Description,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductSpecDto> Specs,
    IReadOnlyList<ProductListItemDto> Related);

public record ProductQuery(
    int Page = 1,
    int PageSize = 12,
    int[]? CategoryIds = null,
    string[]? PriceBands = null, // lt250 | 250-750 | 750-1500 | gt1500
    string? Search = null,
    string? Sort = null);        // featured | price_asc | price_desc | rating
