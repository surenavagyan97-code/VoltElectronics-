using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Catalog.Queries;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Models;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Queries.Catalog;

/// <summary>Shared shape of a storefront product card, reused by every catalog projection.</summary>
internal static class CatalogProjections
{
    /// <summary>
    /// Resolves the display name server-side: the requested language's translation row when one
    /// exists, otherwise the canonical name — so the storefront always receives complete data.
    /// </summary>
    public static Expression<Func<Product, ProductListItemDto>> ListItem(string? lang) =>
        p => new ProductListItemDto(
            p.Id,
            p.Translations.Where(t => t.Lang == lang).Select(t => t.Name).FirstOrDefault() ?? p.Name,
            p.Slug, p.Category.Name, p.CategoryId,
            p.Price, p.CompareAtPrice, p.Badge, p.Rating, p.ReviewCount, p.Stock,
            p.Images.OrderBy(i => i.SortOrder).Select(i => (string?)i.CardUrl).FirstOrDefault());

    public static string? NormalizeLang(string? lang) =>
        string.IsNullOrWhiteSpace(lang) ? null : lang.Trim().ToLowerInvariant();
}

internal sealed class GetProductsHandler(AppDbContext db)
    : IQueryHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    public async Task<PagedResult<ProductListItemDto>> HandleAsync(
        GetProductsQuery query, CancellationToken cancellationToken)
    {
        var q = db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Active);

        if (query.CategoryIds is { Length: > 0 })
            q = q.Where(p => query.CategoryIds.Contains(p.CategoryId));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(term) || p.Category.Name.Contains(term) || p.Sku.Contains(term) ||
                             p.Translations.Any(t => t.Name.Contains(term)));
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

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, maxPageSize: 60);
        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(CatalogProjections.ListItem(CatalogProjections.NormalizeLang(query.Lang)))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>(items, total, page, pageSize);
    }
}

internal sealed class GetProductBySlugHandler(AppDbContext db)
    : IQueryHandler<GetProductBySlugQuery, ProductDetailDto?>
{
    public async Task<ProductDetailDto?> HandleAsync(GetProductBySlugQuery query, CancellationToken cancellationToken)
    {
        var lang = CatalogProjections.NormalizeLang(query.Lang);

        var p = await db.Products.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .Include(x => x.Specs.OrderBy(s => s.SortOrder))
            .Include(x => x.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Slug == query.Slug && x.Status == ProductStatus.Active, cancellationToken);
        if (p is null) return null;

        var related = await db.Products.AsNoTracking()
            .Where(x => x.CategoryId == p.CategoryId && x.Id != p.Id && x.Status == ProductStatus.Active)
            .OrderByDescending(x => x.Rating)
            .Take(4)
            .Select(CatalogProjections.ListItem(lang))
            .ToListAsync(cancellationToken);

        // Too few same-category products? Pad with top-rated picks from elsewhere.
        if (related.Count < 4)
        {
            var excluded = related.Select(r => r.Id).Append(p.Id).ToArray();
            related.AddRange(await db.Products.AsNoTracking()
                .Where(x => !excluded.Contains(x.Id) && x.Status == ProductStatus.Active)
                .OrderByDescending(x => x.Rating)
                .Take(4 - related.Count)
                .Select(CatalogProjections.ListItem(lang))
                .ToListAsync(cancellationToken));
        }

        var translation = p.Translations.FirstOrDefault(t => t.Lang == lang);
        return new ProductDetailDto(
            p.Id, translation?.Name ?? p.Name, p.Slug, p.Sku, p.Category.Name, p.CategoryId,
            p.Price, p.CompareAtPrice, p.Badge, p.Rating, p.ReviewCount, p.Stock,
            translation?.Description ?? p.Description,
            p.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.ThumbUrl, i.CardUrl, i.SortOrder)).ToList(),
            p.Specs.Select(s => new ProductSpecDto(s.Name, s.Value)).ToList(),
            related);
    }
}

internal sealed class GetCategoriesHandler(AppDbContext db)
    : IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(
        GetCategoriesQuery query, CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug,
                db.Products.Count(p => p.CategoryId == c.Id && p.Status == ProductStatus.Active), c.ImageUrl))
            .ToListAsync(cancellationToken);
}

internal sealed class GetProductsByIdsHandler(AppDbContext db)
    : IQueryHandler<GetProductsByIdsQuery, IReadOnlyList<ProductListItemDto>>
{
    public async Task<IReadOnlyList<ProductListItemDto>> HandleAsync(
        GetProductsByIdsQuery query, CancellationToken cancellationToken) =>
        await db.Products.AsNoTracking()
            .Where(p => query.Ids.Contains(p.Id) && p.Status == ProductStatus.Active)
            .OrderBy(p => p.Name)
            .Select(CatalogProjections.ListItem(CatalogProjections.NormalizeLang(query.Lang)))
            .ToListAsync(cancellationToken);
}

internal sealed class GetFeaturedProductsHandler(AppDbContext db)
    : IQueryHandler<GetFeaturedProductsQuery, IReadOnlyList<ProductListItemDto>>
{
    public async Task<IReadOnlyList<ProductListItemDto>> HandleAsync(
        GetFeaturedProductsQuery query, CancellationToken cancellationToken) =>
        await db.Products.AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active)
            .OrderByDescending(p => p.Badge != null)
            .ThenByDescending(p => p.Rating)
            .Take(Math.Clamp(query.Count, 1, 12))
            .Select(CatalogProjections.ListItem(CatalogProjections.NormalizeLang(query.Lang)))
            .ToListAsync(cancellationToken);
}
