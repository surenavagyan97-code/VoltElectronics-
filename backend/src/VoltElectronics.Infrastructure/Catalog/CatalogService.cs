using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common;
using VoltElectronics.Domain.Entities;
using VoltElectronics.Domain.Enums;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Catalog;

public class CatalogService(AppDbContext db) : ICatalogService
{
    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(ProductQuery query)
    {
        var q = db.Products.Where(p => p.Status == ProductStatus.Active);

        if (query.CategoryIds is { Length: > 0 })
            q = q.Where(p => query.CategoryIds.Contains(p.CategoryId));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(term) || p.Category.Name.Contains(term) || p.Sku.Contains(term));
        }

        if (query.PriceBands is { Length: > 0 })
        {
            var bands = query.PriceBands;
            q = q.Where(p =>
                (bands.Contains("lt250") && p.Price < 250) ||
                (bands.Contains("250-750") && p.Price >= 250 && p.Price <= 750) ||
                (bands.Contains("750-1500") && p.Price > 750 && p.Price <= 1500) ||
                (bands.Contains("gt1500") && p.Price > 1500));
        }

        q = query.Sort switch
        {
            "price_asc" => q.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "price_desc" => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            "rating" => q.OrderByDescending(p => p.Rating).ThenBy(p => p.Id),
            // "featured": badges first, then best-rated.
            _ => q.OrderByDescending(p => p.Badge != null).ThenByDescending(p => p.Rating).ThenBy(p => p.Id)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 60);
        var total = await q.CountAsync();
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ListItemProjection)
            .ToListAsync();

        return new PagedResult<ProductListItemDto>(items, total, page, pageSize);
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var p = await db.Products
            .Include(x => x.Category)
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .Include(x => x.Specs.OrderBy(s => s.SortOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Status == ProductStatus.Active);
        if (p is null) return null;

        var related = await db.Products
            .Where(x => x.CategoryId == p.CategoryId && x.Id != p.Id && x.Status == ProductStatus.Active)
            .OrderByDescending(x => x.Rating)
            .Take(4)
            .Select(ListItemProjection)
            .ToListAsync();

        // Too few same-category products? Pad with top-rated picks from elsewhere.
        if (related.Count < 4)
        {
            var excluded = related.Select(r => r.Id).Append(p.Id).ToArray();
            related.AddRange(await db.Products
                .Where(x => !excluded.Contains(x.Id) && x.Status == ProductStatus.Active)
                .OrderByDescending(x => x.Rating)
                .Take(4 - related.Count)
                .Select(ListItemProjection)
                .ToListAsync());
        }

        return new ProductDetailDto(
            p.Id, p.Name, p.Slug, p.Sku, p.Category.Name, p.CategoryId,
            p.Price, p.CompareAtPrice, p.Badge, p.Rating, p.ReviewCount, p.Stock, p.Description,
            p.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.SortOrder)).ToList(),
            p.Specs.Select(s => new ProductSpecDto(s.Name, s.Value)).ToList(),
            related);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync() =>
        await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug,
                c.Products.Count(p => p.Status == ProductStatus.Active)))
            .ToListAsync();

    public async Task<IReadOnlyList<ProductListItemDto>> GetFeaturedAsync(int count = 4) =>
        await db.Products
            .Where(p => p.Status == ProductStatus.Active)
            .OrderByDescending(p => p.Badge != null)
            .ThenByDescending(p => p.Rating)
            .Take(count)
            .Select(ListItemProjection)
            .ToListAsync();

    internal static readonly System.Linq.Expressions.Expression<Func<Product, ProductListItemDto>> ListItemProjection =
        p => new ProductListItemDto(
            p.Id, p.Name, p.Slug, p.Category.Name, p.CategoryId,
            p.Price, p.CompareAtPrice, p.Badge, p.Rating, p.ReviewCount, p.Stock,
            p.Images.OrderBy(i => i.SortOrder).Select(i => (string?)i.Url).FirstOrDefault());
}
