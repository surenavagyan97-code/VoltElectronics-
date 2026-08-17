using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Queries;
using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Models;
using VoltElectronics.Application.Promotions;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Ordering;
using VoltElectronics.Domain.Promotions;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Infrastructure.Queries.Admin;

internal static class AdminProjections
{
    public static readonly Expression<Func<Product, AdminProductListItemDto>> ProductListItem =
        p => new AdminProductListItemDto(
            p.Id, p.Name, p.Slug, p.Sku, p.Category.Name, p.CategoryId,
            p.Price, p.CompareAtPrice, p.Stock, p.Status.ToString(), p.Badge,
            p.Images.OrderBy(i => i.SortOrder).Select(i => (string?)i.CardUrl).FirstOrDefault());
}

internal sealed class AdminGetProductsHandler(AppDbContext db)
    : IQueryHandler<AdminGetProductsQuery, PagedResult<AdminProductListItemDto>>
{
    public async Task<PagedResult<AdminProductListItemDto>> HandleAsync(
        AdminGetProductsQuery query, CancellationToken cancellationToken)
    {
        var q = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(term) || p.Sku.Contains(term) || p.Category.Name.Contains(term));
        }

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, maxPageSize: 100);
        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(AdminProjections.ProductListItem)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminProductListItemDto>(items, total, page, pageSize);
    }
}

internal sealed class AdminGetProductHandler(AppDbContext db)
    : IQueryHandler<AdminGetProductQuery, AdminProductDetailDto?>
{
    public async Task<AdminProductDetailDto?> HandleAsync(
        AdminGetProductQuery query, CancellationToken cancellationToken)
    {
        var p = await db.Products.AsNoTracking()
            .Include(x => x.Images.OrderBy(i => i.SortOrder))
            .Include(x => x.Specs.OrderBy(s => s.SortOrder))
            .Include(x => x.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
        if (p is null) return null;

        return new AdminProductDetailDto(
            p.Id, p.Name, p.Slug, p.Sku, p.CategoryId, p.Description,
            p.Price, p.CompareAtPrice, p.Stock, p.Status.ToString(), p.Badge,
            p.Rating, p.ReviewCount,
            p.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.ThumbUrl, i.CardUrl, i.SortOrder)).ToList(),
            p.Specs.Select(s => new ProductSpecDto(s.Name, s.Value)).ToList(),
            p.Translations.Select(t => new ProductTranslationDto(t.Lang, t.Name, t.Description)).ToList());
    }
}

internal sealed class ExportProductsHandler(AppDbContext db)
    : IQueryHandler<ExportProductsQuery, IReadOnlyList<ProductExportRowDto>>
{
    public async Task<IReadOnlyList<ProductExportRowDto>> HandleAsync(
        ExportProductsQuery query, CancellationToken cancellationToken)
    {
        // The specs cell is a joined string, which SQL can't produce — project the raw
        // shape first, then format in memory.
        var rows = await db.Products.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id, p.Name, p.Sku, Category = p.Category.Name, p.Description,
                p.Price, p.CompareAtPrice, p.Stock, p.Status, p.Badge, p.Rating, p.ReviewCount,
                Specs = p.Specs.OrderBy(s => s.SortOrder).Select(s => new { s.Name, s.Value }).ToList(),
                Translations = p.Translations.Select(t => new { t.Lang, t.Name, t.Description }).ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(p => new ProductExportRowDto(
                p.Id, p.Name, p.Sku, p.Category, p.Description,
                p.Price, p.CompareAtPrice, p.Stock, p.Status.ToString(), p.Badge,
                p.Rating, p.ReviewCount,
                string.Join("\n", p.Specs.Select(s => $"{s.Name}: {s.Value}")),
                p.Translations.Select(t => new ProductTranslationDto(t.Lang, t.Name, t.Description)).ToList()))
            .ToList();
    }
}

internal sealed class AdminGetCategoriesHandler(AppDbContext db)
    : IQueryHandler<AdminGetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(
        AdminGetCategoriesQuery query, CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug,
                db.Products.Count(p => p.CategoryId == c.Id), c.ImageUrl))
            .ToListAsync(cancellationToken);
}

internal sealed class AdminGetOrdersHandler(AppDbContext db)
    : IQueryHandler<AdminGetOrdersQuery, PagedResult<AdminOrderListItemDto>>
{
    public async Task<PagedResult<AdminOrderListItemDto>> HandleAsync(
        AdminGetOrdersQuery query, CancellationToken cancellationToken)
    {
        var q = db.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var parsed))
            q = q.Where(o => o.Status == parsed);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(term) || o.ShipTo.FullName.Contains(term) ||
                             (o.GuestEmail != null && o.GuestEmail.Contains(term)) ||
                             (o.ShipTo.Phone != null && o.ShipTo.Phone.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(query.CourierId))
            q = q.Where(o => o.AssignedCourierId == query.CourierId);

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize, maxPageSize: 100);
        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new AdminOrderListItemDto(
                o.OrderNumber, o.ShipTo.FullName, o.GuestEmail ?? "", o.ShipTo.Phone, o.CreatedAt,
                o.Totals.Total, o.Totals.Currency, o.Status.ToString(), o.Items.Sum(i => i.Qty),
                o.AssignedCourierId,
                db.Users.Where(u => u.Id == o.AssignedCourierId).Select(u => u.FullName).FirstOrDefault(),
                o.CouponCode))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminOrderListItemDto>(items, total, page, pageSize);
    }
}

internal sealed class AdminGetOrderStatsHandler(AppDbContext db)
    : IQueryHandler<AdminGetOrderStatsQuery, AdminOrderStatsDto>
{
    public async Task<AdminOrderStatsDto> HandleAsync(
        AdminGetOrderStatsQuery query, CancellationToken cancellationToken)
    {
        var counts = await db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        int C(OrderStatus s) => counts.GetValueOrDefault(s);
        return new AdminOrderStatsDto(
            counts.Values.Sum(), C(OrderStatus.PendingPayment), C(OrderStatus.Processing),
            C(OrderStatus.Shipped), C(OrderStatus.Delivered), C(OrderStatus.Cancelled));
    }
}

internal sealed class AdminGetCouriersHandler(AppDbContext db)
    : IQueryHandler<AdminGetCouriersQuery, IReadOnlyList<CourierDto>>
{
    public async Task<IReadOnlyList<CourierDto>> HandleAsync(
        AdminGetCouriersQuery query, CancellationToken cancellationToken) =>
        await (
            from user in db.Users.AsNoTracking()
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            where role.Name == Application.Identity.Roles.Courier
            orderby user.FullName
            select new CourierDto(
                user.Id, user.Email ?? "", user.FullName ?? "",
                db.Orders.Count(o => o.AssignedCourierId == user.Id &&
                                     (o.Status == OrderStatus.Processing || o.Status == OrderStatus.Shipped))))
            .ToListAsync(cancellationToken);
}

internal sealed class GetAnalyticsHandler(AppDbContext db) : IQueryHandler<GetAnalyticsQuery, AnalyticsDto>
{
    public async Task<AnalyticsDto> HandleAsync(GetAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var d30 = now.AddDays(-30);
        var d60 = now.AddDays(-60);

        // Revenue = paid orders only (Cancelled/PendingPayment excluded via PaidAt). Orders can be
        // in different currencies, so every sum divides by the order's frozen ExchangeRate first to
        // normalize back to the store's base currency — otherwise USD/EUR/AMD totals would just add
        // face values together, which is meaningless.
        var paid = db.Orders.AsNoTracking().Where(o => o.PaidAt != null);

        var current = await paid.Where(o => o.CreatedAt >= d30)
            .GroupBy(_ => 1)
            .Select(g => new { Revenue = g.Sum(o => o.Totals.Total / o.Totals.ExchangeRate), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);
        var previous = await paid.Where(o => o.CreatedAt >= d60 && o.CreatedAt < d30)
            .GroupBy(_ => 1)
            .Select(g => new { Revenue = g.Sum(o => o.Totals.Total / o.Totals.ExchangeRate), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

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
            .Select(g => new { Day = g.Key, Revenue = g.Sum(o => o.Totals.Total / o.Totals.ExchangeRate), Orders = g.Count() })
            .ToListAsync(cancellationToken);
        var revenueByDay = Enumerable.Range(0, 7)
            .Select(i => d7.AddDays(i))
            .Select(day =>
            {
                var hit = revenueByDayRaw.FirstOrDefault(r => DateOnly.FromDateTime(r.Day) == day);
                return new RevenueDayDto(day, hit?.Revenue ?? 0, hit?.Orders ?? 0);
            })
            .ToList();

        var topProducts = (await paid
                .Where(o => o.CreatedAt >= d30)
                .SelectMany(o => o.Items.Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.Qty,
                    Revenue = i.UnitPrice * i.Qty / o.Totals.ExchangeRate
                }))
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    Units = g.Sum(x => x.Qty),
                    Revenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(t => t.Revenue)
                .Take(5)
                .ToListAsync(cancellationToken))
            .Select(t => new TopProductDto(t.ProductId, t.ProductName, t.Units, t.Revenue))
            .ToList();

        var lowStock = await db.Products.AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active && p.Stock < 20)
            .OrderBy(p => p.Stock)
            .Select(AdminProjections.ProductListItem)
            .ToListAsync(cancellationToken);

        return new AnalyticsDto(
            revenue30, Delta(revenue30, prevRevenue),
            orders30, Delta(orders30, prevOrders),
            orders30 == 0 ? 0 : Math.Round(revenue30 / orders30, 2),
            lowStock.Count, revenueByDay, topProducts, lowStock);
    }
}

internal sealed class AdminGetPromotionsHandler(IPromotionRepository promotions, AppDbContext db)
    : IQueryHandler<AdminGetPromotionsQuery, IReadOnlyList<PromotionDto>>
{
    public async Task<IReadOnlyList<PromotionDto>> HandleAsync(
        AdminGetPromotionsQuery query, CancellationToken cancellationToken)
    {
        var all = await promotions.GetAllAsync(cancellationToken);

        var categoryIds = all.Where(p => p.CategoryId is not null).Select(p => p.CategoryId!.Value).Distinct().ToArray();
        var categoryNames = await db.Categories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return all
            .Select(p => new PromotionDto(
                p.Id, p.Code, p.Name, p.Type.ToString(), p.Value, p.Scope.ToString(),
                p.CategoryId, p.CategoryId is not null ? categoryNames.GetValueOrDefault(p.CategoryId.Value) : null,
                p.Products.Select(x => x.ProductId).ToList(),
                p.MinSubtotal, p.MaxDiscountAmount, p.MaxRedemptions, p.RedemptionCount,
                p.StartsAt, p.ExpiresAt, p.IsActive, p.CreatedAt))
            .ToList();
    }
}
