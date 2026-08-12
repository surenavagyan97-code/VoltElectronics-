using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Catalog.Queries;

/// <summary>Storefront category nav — counts only active products.</summary>
public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;
