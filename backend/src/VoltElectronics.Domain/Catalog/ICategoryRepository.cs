namespace VoltElectronics.Domain.Catalog;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? exceptCategoryId = null, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default);
    void Add(Category category);
    void Remove(Category category);
}
