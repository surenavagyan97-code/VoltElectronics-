using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common;
using VoltElectronics.Domain.Entities;
using VoltElectronics.Domain.Enums;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Admin;

public class AdminService(AppDbContext db) : IAdminService
{
    // ---------- Products ----------

    public async Task<PagedResult<AdminProductListItemDto>> GetProductsAsync(int page, int pageSize, string? search)
    {
        var q = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(p => p.Name.Contains(term) || p.Sku.Contains(term) || p.Category.Name.Contains(term));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await q.CountAsync();
        var items = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new AdminProductListItemDto(
                p.Id, p.Name, p.Slug, p.Sku, p.Category.Name, p.CategoryId,
                p.Price, p.CompareAtPrice, p.Stock, p.Status.ToString(), p.Badge,
                p.Images.OrderBy(i => i.SortOrder).Select(i => (string?)i.CardUrl).FirstOrDefault()))
            .ToListAsync();
        return new PagedResult<AdminProductListItemDto>(items, total, page, pageSize);
    }

    public async Task<AdminProductDetailDto?> GetProductAsync(int id)
    {
        var p = await db.Products
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .Include(x => x.Specs.OrderBy(s => s.SortOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;
        return new AdminProductDetailDto(
            p.Id, p.Name, p.Slug, p.Sku, p.CategoryId, p.Description,
            p.Price, p.CompareAtPrice, p.Stock, p.Status.ToString(), p.Badge,
            p.Rating, p.ReviewCount,
            p.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.ThumbUrl, i.CardUrl, i.SortOrder)).ToList(),
            p.Specs.Select(s => new ProductSpecDto(s.Name, s.Value)).ToList());
    }

    public async Task<(AdminResult Result, int? Id)> CreateProductAsync(SaveProductRequest request)
    {
        var error = await ValidateProductAsync(request, existingId: null);
        if (error is not null) return (AdminResult.Fail(error), null);

        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = await UniqueSlugAsync(request.Name),
            Sku = request.Sku.Trim(),
            CategoryId = request.CategoryId,
            Description = request.Description ?? "",
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            Stock = request.Stock,
            Status = ParseStatus(request.Status),
            Badge = NullIfEmpty(request.Badge),
            Specs = MapSpecs(request.Specs)
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return (AdminResult.Ok, product.Id);
    }

    public async Task<AdminResult> UpdateProductAsync(int id, SaveProductRequest request)
    {
        var product = await db.Products.Include(p => p.Specs).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return AdminResult.Fail("Product not found.");

        var error = await ValidateProductAsync(request, id);
        if (error is not null) return AdminResult.Fail(error);

        if (!string.Equals(product.Name, request.Name.Trim(), StringComparison.Ordinal))
            product.Slug = await UniqueSlugAsync(request.Name, id);

        product.Name = request.Name.Trim();
        product.Sku = request.Sku.Trim();
        product.CategoryId = request.CategoryId;
        product.Description = request.Description ?? "";
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.Stock = request.Stock;
        product.Status = ParseStatus(request.Status);
        product.Badge = NullIfEmpty(request.Badge);

        db.ProductSpecs.RemoveRange(product.Specs);
        product.Specs = MapSpecs(request.Specs);

        await db.SaveChangesAsync();
        return AdminResult.Ok;
    }

    public async Task<AdminResult> DeleteProductAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return AdminResult.Fail("Product not found.");

        var hasOrders = await db.OrderItems.AnyAsync(i => i.ProductId == id);
        if (hasOrders)
        {
            product.Status = ProductStatus.Archived;   // keep order history intact
        }
        else
        {
            db.CartItems.RemoveRange(db.CartItems.Where(ci => ci.ProductId == id));
            db.Products.Remove(product);
        }
        await db.SaveChangesAsync();
        return AdminResult.Ok;
    }

    public async Task<(AdminResult Result, ProductImageDto? Image)> AddProductImageAsync(
        int productId, string url, string thumbUrl, string cardUrl)
    {
        var product = await db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return (AdminResult.Fail("Product not found."), null);

        var image = new ProductImage
        {
            Url = url,
            ThumbUrl = thumbUrl,
            CardUrl = cardUrl,
            SortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1
        };
        product.Images.Add(image);
        await db.SaveChangesAsync();
        return (AdminResult.Ok, new ProductImageDto(image.Id, image.Url, image.ThumbUrl, image.CardUrl, image.SortOrder));
    }

    public async Task<AdminResult> RemoveProductImageAsync(int productId, int imageId)
    {
        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);
        if (image is null) return AdminResult.Fail("Image not found.");
        db.ProductImages.Remove(image);
        await db.SaveChangesAsync();
        return AdminResult.Ok;
    }

    // ---------- Categories ----------

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync() =>
        await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Products.Count))
            .ToListAsync();

    public async Task<(AdminResult Result, CategoryDto? Category)> CreateCategoryAsync(SaveCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (AdminResult.Fail("Name is required."), null);
        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name))
            return (AdminResult.Fail("A category with this name already exists."), null);

        var category = new Category { Name = name, Slug = Slug.From(name) };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return (AdminResult.Ok, new CategoryDto(category.Id, category.Name, category.Slug, 0));
    }

    public async Task<AdminResult> UpdateCategoryAsync(int id, SaveCategoryRequest request)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return AdminResult.Fail("Category not found.");
        if (string.IsNullOrWhiteSpace(request.Name)) return AdminResult.Fail("Name is required.");

        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name && c.Id != id))
            return AdminResult.Fail("A category with this name already exists.");

        category.Name = name;
        category.Slug = Slug.From(name);
        await db.SaveChangesAsync();
        return AdminResult.Ok;
    }

    public async Task<AdminResult> DeleteCategoryAsync(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return AdminResult.Fail("Category not found.");
        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            return AdminResult.Fail("Category has products — move or delete them first.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return AdminResult.Ok;
    }

    // ---------- Orders ----------

    public async Task<PagedResult<AdminOrderListItemDto>> GetOrdersAsync(int page, int pageSize, string? status, string? search)
    {
        var q = db.Orders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
            q = q.Where(o => o.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(term) || o.ShipFullName.Contains(term) ||
                             (o.GuestEmail != null && o.GuestEmail.Contains(term)));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new AdminOrderListItemDto(
                o.OrderNumber, o.ShipFullName, o.GuestEmail ?? "", o.CreatedAt,
                o.Total, o.Currency, o.Status.ToString(), o.Items.Sum(i => i.Qty)))
            .ToListAsync();
        return new PagedResult<AdminOrderListItemDto>(items, total, page, pageSize);
    }

    public async Task<AdminOrderStatsDto> GetOrderStatsAsync()
    {
        var counts = await db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        int C(OrderStatus s) => counts.GetValueOrDefault(s);
        return new AdminOrderStatsDto(
            counts.Values.Sum(), C(OrderStatus.PendingPayment), C(OrderStatus.Processing),
            C(OrderStatus.Shipped), C(OrderStatus.Delivered), C(OrderStatus.Cancelled));
    }

    public async Task<AdminResult> UpdateOrderStatusAsync(string orderNumber, string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var parsed))
            return AdminResult.Fail($"Unknown status \"{status}\".");
        var order = await db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (order is null) return AdminResult.Fail("Order not found.");
        order.Status = parsed;
        await db.SaveChangesAsync();
        return AdminResult.Ok;
    }

    // ---------- Analytics ----------

    public async Task<AnalyticsDto> GetAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var d30 = now.AddDays(-30);
        var d60 = now.AddDays(-60);

        // Revenue = paid orders only (Cancelled/PendingPayment excluded via PaidAt). Orders can be
        // in different currencies, so every sum divides by the order's frozen ExchangeRate first to
        // normalize back to the store's base currency — otherwise USD/EUR/AMD totals would just add
        // face values together, which is meaningless.
        var paid = db.Orders.Where(o => o.PaidAt != null);

        var current = await paid.Where(o => o.CreatedAt >= d30)
            .GroupBy(_ => 1)
            .Select(g => new { Revenue = g.Sum(o => o.Total / o.ExchangeRate), Count = g.Count() })
            .FirstOrDefaultAsync();
        var previous = await paid.Where(o => o.CreatedAt >= d60 && o.CreatedAt < d30)
            .GroupBy(_ => 1)
            .Select(g => new { Revenue = g.Sum(o => o.Total / o.ExchangeRate), Count = g.Count() })
            .FirstOrDefaultAsync();

        var revenue30 = current?.Revenue ?? 0;
        var orders30 = current?.Count ?? 0;
        var prevRevenue = previous?.Revenue ?? 0;
        var prevOrders = previous?.Count ?? 0;

        static double Delta(decimal cur, decimal prev) =>
            prev == 0 ? 0 : Math.Round((double)((cur - prev) / prev) * 100, 1);

        var d7 = DateOnly.FromDateTime(now.AddDays(-6));
        var revenueByDayRaw = await paid
            .Where(o => o.CreatedAt >= now.AddDays(-7))
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Revenue = g.Sum(o => o.Total / o.ExchangeRate), Orders = g.Count() })
            .ToListAsync();
        var revenueByDay = Enumerable.Range(0, 7)
            .Select(i => d7.AddDays(i))
            .Select(day =>
            {
                var hit = revenueByDayRaw.FirstOrDefault(r => DateOnly.FromDateTime(r.Day) == day);
                return new RevenueDayDto(day, hit?.Revenue ?? 0, hit?.Orders ?? 0);
            })
            .ToList();

        var topProducts = (await db.OrderItems
                .Where(i => i.Order.PaidAt != null && i.Order.CreatedAt >= d30)
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    Units = g.Sum(i => i.Qty),
                    Revenue = g.Sum(i => i.UnitPrice * i.Qty / i.Order.ExchangeRate)
                })
                .OrderByDescending(t => t.Revenue)
                .Take(5)
                .ToListAsync())
            .Select(t => new TopProductDto(t.ProductId, t.ProductName, t.Units, t.Revenue))
            .ToList();

        var lowStock = await db.Products
            .Where(p => p.Status == ProductStatus.Active && p.Stock < 20)
            .OrderBy(p => p.Stock)
            .Select(p => new AdminProductListItemDto(
                p.Id, p.Name, p.Slug, p.Sku, p.Category.Name, p.CategoryId,
                p.Price, p.CompareAtPrice, p.Stock, p.Status.ToString(), p.Badge,
                p.Images.OrderBy(i => i.SortOrder).Select(i => (string?)i.CardUrl).FirstOrDefault()))
            .ToListAsync();

        return new AnalyticsDto(
            revenue30, Delta(revenue30, prevRevenue),
            orders30, Delta(orders30, prevOrders),
            orders30 == 0 ? 0 : Math.Round(revenue30 / orders30, 2),
            lowStock.Count, revenueByDay, topProducts, lowStock);
    }

    // ---------- helpers ----------

    private async Task<string?> ValidateProductAsync(SaveProductRequest request, int? existingId)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
        if (string.IsNullOrWhiteSpace(request.Sku)) return "SKU is required.";
        if (request.Price <= 0) return "Price must be positive.";
        if (request.Stock < 0) return "Stock cannot be negative.";
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId)) return "Category not found.";
        var sku = request.Sku.Trim();
        if (await db.Products.AnyAsync(p => p.Sku == sku && p.Id != existingId)) return "SKU already in use.";
        return null;
    }

    private async Task<string> UniqueSlugAsync(string name, int? existingId = null)
    {
        var baseSlug = Slug.From(name);
        var slug = baseSlug;
        for (var i = 2; await db.Products.AnyAsync(p => p.Slug == slug && p.Id != existingId); i++)
            slug = $"{baseSlug}-{i}";
        return slug;
    }

    private static ProductStatus ParseStatus(string status) =>
        Enum.TryParse<ProductStatus>(status, true, out var parsed) ? parsed : ProductStatus.Active;

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static List<ProductSpec> MapSpecs(List<ProductSpecDto>? specs) =>
        (specs ?? [])
        .Where(s => !string.IsNullOrWhiteSpace(s.Name) && !string.IsNullOrWhiteSpace(s.Value))
        .Select((s, i) => new ProductSpec { Name = s.Name.Trim(), Value = s.Value.Trim(), SortOrder = i })
        .ToList();
}
