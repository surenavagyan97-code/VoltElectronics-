using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Catalog;

namespace VoltElectronics.Infrastructure.Data.Repositories;

internal sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        db.Categories.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? exceptCategoryId = null, CancellationToken cancellationToken = default) =>
        db.Categories.AnyAsync(c => c.Name == name && c.Id != exceptCategoryId, cancellationToken);

    public Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        db.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken);

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
