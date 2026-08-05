using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common;

namespace VoltElectronics.Application.Admin;

public interface IAdminService
{
    // Products (all statuses, unlike the public catalog)
    Task<PagedResult<AdminProductListItemDto>> GetProductsAsync(int page, int pageSize, string? search);
    Task<AdminProductDetailDto?> GetProductAsync(int id);
    Task<(AdminResult Result, int? Id)> CreateProductAsync(SaveProductRequest request);
    Task<AdminResult> UpdateProductAsync(int id, SaveProductRequest request);
    /// <summary>Archives when the product has order history; hard-deletes otherwise.</summary>
    Task<AdminResult> DeleteProductAsync(int id);
    Task<(AdminResult Result, ProductImageDto? Image)> AddProductImageAsync(int productId, string url);
    Task<AdminResult> RemoveProductImageAsync(int productId, int imageId);

    // Categories
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();
    Task<(AdminResult Result, CategoryDto? Category)> CreateCategoryAsync(SaveCategoryRequest request);
    Task<AdminResult> UpdateCategoryAsync(int id, SaveCategoryRequest request);
    Task<AdminResult> DeleteCategoryAsync(int id);

    // Orders
    Task<PagedResult<AdminOrderListItemDto>> GetOrdersAsync(int page, int pageSize, string? status, string? search);
    Task<AdminOrderStatsDto> GetOrderStatsAsync();
    Task<AdminResult> UpdateOrderStatusAsync(string orderNumber, string status);

    // Analytics
    Task<AnalyticsDto> GetAnalyticsAsync();
}
