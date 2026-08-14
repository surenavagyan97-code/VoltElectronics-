using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Catalog.Queries;

/// <summary>
/// The shopper's favorites list — ids live in the browser, so the page asks for exactly those
/// products. Unknown or inactive ids are silently dropped.
/// </summary>
public sealed record GetProductsByIdsQuery(int[] Ids) : IQuery<IReadOnlyList<ProductListItemDto>>;
