using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Catalog.Queries;

/// <summary>
/// The shopper's compare list — ids live in the browser, so the page asks for exactly those
/// products, specs included. Unknown or inactive ids are silently dropped.
/// </summary>
public sealed record GetProductsForCompareQuery(int[] Ids, string? Lang = null) : IQuery<IReadOnlyList<ProductDetailDto>>;
