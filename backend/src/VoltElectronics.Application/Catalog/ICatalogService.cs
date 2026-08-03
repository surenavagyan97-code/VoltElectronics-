using VoltElectronics.Application.Common;

namespace VoltElectronics.Application.Catalog;

public interface ICatalogService
{
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(ProductQuery query);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();
    Task<IReadOnlyList<ProductListItemDto>> GetFeaturedAsync(int count = 4);
}
