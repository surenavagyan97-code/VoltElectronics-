using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetAggregateAsync(int id, CancellationToken cancellationToken = default) =>
        db.Products
            .Include(p => p.Images)
            .Include(p => p.Specs)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
        await db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, int? exceptProductId = null, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.Sku == sku && p.Id != exceptProductId, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, int? exceptProductId = null, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.Slug == slug && p.Id != exceptProductId, cancellationToken);

    public Task<bool> HasOrderHistoryAsync(int productId, CancellationToken cancellationToken = default) =>
        db.Set<OrderItem>().AnyAsync(i => i.ProductId == productId, cancellationToken);

    public void Add(Product product) => db.Products.Add(product);

    public void Remove(Product product) => db.Products.Remove(product);
}
