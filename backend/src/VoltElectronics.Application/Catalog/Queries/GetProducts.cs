using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Models;

namespace VoltElectronics.Application.Catalog.Queries;

/// <summary>Storefront product listing — active products only.</summary>
/// <param name="PriceBands">Any of <c>lt250</c>, <c>250-750</c>, <c>750-1500</c>, <c>gt1500</c>; OR-ed together.</param>
/// <param name="Sort"><c>featured</c> (default) | <c>price_asc</c> | <c>price_desc</c> | <c>rating</c>.</param>
public sealed record GetProductsQuery(
    int Page = 1,
    int PageSize = 12,
    int[]? CategoryIds = null,
    string[]? PriceBands = null,
    string? Search = null,
    string? Sort = null) : IQuery<PagedResult<ProductListItemDto>>;
