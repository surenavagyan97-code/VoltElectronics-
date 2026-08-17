using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Models;

namespace VoltElectronics.Application.Admin.Queries;

// Admin reads span every product status and every order, unlike their storefront counterparts.
// All of these project straight from the database, so their handlers live in the persistence layer.

public sealed record AdminGetProductsQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IQuery<PagedResult<AdminProductListItemDto>>;

public sealed record AdminGetProductQuery(int Id) : IQuery<AdminProductDetailDto?>;

/// <summary>The full catalog, one row per product, for the Excel export.</summary>
public sealed record ExportProductsQuery : IQuery<IReadOnlyList<ProductExportRowDto>>;

/// <summary>Every category with its total product count — the storefront's version counts active only.</summary>
public sealed record AdminGetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;

public sealed record AdminGetOrdersQuery(
    int Page = 1, int PageSize = 20, string? Status = null, string? Search = null, string? CourierId = null)
    : IQuery<PagedResult<AdminOrderListItemDto>>;

public sealed record AdminGetOrderStatsQuery : IQuery<AdminOrderStatsDto>;

/// <summary>Every delivery-person account, with how many undelivered orders each one carries.</summary>
public sealed record AdminGetCouriersQuery : IQuery<IReadOnlyList<CourierDto>>;

public sealed record GetAnalyticsQuery : IQuery<AnalyticsDto>;
