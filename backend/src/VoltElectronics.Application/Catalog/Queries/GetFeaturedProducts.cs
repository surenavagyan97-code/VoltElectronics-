using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Catalog.Queries;

public sealed record GetFeaturedProductsQuery(int Count = 4, string? Lang = null) : IQuery<IReadOnlyList<ProductListItemDto>>;
