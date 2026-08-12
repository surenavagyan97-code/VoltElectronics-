namespace VoltElectronics.Domain.Catalog;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Loads the full aggregate (images + specs) for write operations.</summary>
    Task<Product?> GetAggregateAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, int? exceptProductId = null, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? exceptProductId = null, CancellationToken cancellationToken = default);
    Task<bool> HasOrderHistoryAsync(int productId, CancellationToken cancellationToken = default);
    void Add(Product product);
    void Remove(Product product);
}
