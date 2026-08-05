using VoltElectronics.Application.Catalog;

namespace VoltElectronics.Application.Admin;

// Products
public record AdminProductListItemDto(
    int Id, string Name, string Slug, string Sku, string Category, int CategoryId,
    decimal Price, decimal? CompareAtPrice, int Stock, string Status, string? Badge, string? ImageUrl);

public record AdminProductDetailDto(
    int Id, string Name, string Slug, string Sku, int CategoryId, string Description,
    decimal Price, decimal? CompareAtPrice, int Stock, string Status, string? Badge,
    double Rating, int ReviewCount,
    IReadOnlyList<ProductImageDto> Images, IReadOnlyList<ProductSpecDto> Specs);

public record SaveProductRequest(
    string Name, string Sku, int CategoryId, string Description,
    decimal Price, decimal? CompareAtPrice, int Stock, string Status, string? Badge,
    List<ProductSpecDto>? Specs);

// Categories
public record SaveCategoryRequest(string Name);

// Orders
public record AdminOrderListItemDto(
    string OrderNumber, string Customer, string Email, DateTime CreatedAt,
    decimal Total, string Status, int ItemCount);

public record UpdateOrderStatusRequest(string Status);

public record AdminOrderStatsDto(int Total, int PendingPayment, int Processing, int Shipped, int Delivered, int Cancelled);

// Analytics
public record RevenueDayDto(DateOnly Day, decimal Revenue, int Orders);
public record TopProductDto(int ProductId, string Name, int UnitsSold, decimal Revenue);
public record AnalyticsDto(
    decimal Revenue30d, double RevenueDeltaPct,
    int Orders30d, double OrdersDeltaPct,
    decimal AverageOrderValue30d,
    int LowStockCount,
    IReadOnlyList<RevenueDayDto> RevenueByDay7d,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<AdminProductListItemDto> LowStockProducts);

public record AdminResult(bool Success, string? Error)
{
    public static readonly AdminResult Ok = new(true, null);
    public static AdminResult Fail(string error) => new(false, error);
}
