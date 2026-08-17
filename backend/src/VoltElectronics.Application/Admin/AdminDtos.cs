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
    IReadOnlyList<ProductImageDto> Images, IReadOnlyList<ProductSpecDto> Specs,
    IReadOnlyList<ProductTranslationDto> Translations);

public record SaveProductRequest(
    string Name, string Sku, int CategoryId, string Description,
    decimal Price, decimal? CompareAtPrice, int Stock, string Status, string? Badge,
    List<ProductSpecDto>? Specs, List<ProductTranslationDto>? Translations = null);

// Product Excel import/export. The export row doubles as the import template — SKU is the
// natural key rows are matched on, and Specs round-trip as "Name: Value" lines in one cell.
public record ProductExportRowDto(
    int Id, string Name, string Sku, string Category, string Description,
    decimal Price, decimal? CompareAtPrice, int Stock, string Status, string? Badge,
    double Rating, int ReviewCount, string Specs,
    IReadOnlyList<ProductTranslationDto> Translations);

/// <summary>
/// One parsed spreadsheet row; RowNumber is the Excel row it came from, for error reporting.
/// Translations is null when the file has no "Name (xx)" columns — meaning "leave unchanged".
/// </summary>
public record ImportProductRow(
    int RowNumber, string? Name, string? Sku, string? Category, string? Description,
    decimal? Price, decimal? CompareAtPrice, int? Stock, string? Status, string? Badge,
    double? Rating, int? ReviewCount, string? Specs,
    List<ProductTranslationDto>? Translations = null);

public record ImportRowError(int RowNumber, string Error);

public record ImportProductsResultDto(int Created, int Updated, IReadOnlyList<ImportRowError> Errors);

// Categories
public record SaveCategoryRequest(string Name);

// Editable storefront pages
public record SaveContentRequest(string Lang, string Body);

// Orders
public record AdminOrderListItemDto(
    string OrderNumber, string Customer, string Email, string? Phone, DateTime CreatedAt,
    decimal Total, string Currency, string Status, int ItemCount,
    string? CourierId, string? CourierName);

public record UpdateOrderStatusRequest(string Status);

// Couriers (delivery people)
public record CourierDto(string Id, string Email, string FullName, int ActiveOrderCount);

public record CreateCourierRequest(string Email, string Password, string FullName);

/// <summary>Null courier id unassigns the order.</summary>
public record AssignOrderCourierRequest(string? CourierId);

public record AdminOrderStatsDto(int Total, int PendingPayment, int Processing, int Shipped, int Delivered, int Cancelled);

// Analytics — all revenue figures are normalized to the store's base currency (see
// CurrencyOptions.Base), regardless of what currency individual orders were charged in.
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
