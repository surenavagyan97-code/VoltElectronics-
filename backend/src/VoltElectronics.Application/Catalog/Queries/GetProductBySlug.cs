using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Application.Catalog.Queries;

/// <summary>Product detail page. Null when the slug is unknown or the product isn't active.</summary>
public sealed record GetProductBySlugQuery(string Slug, string? Lang = null) : IQuery<ProductDetailDto?>;
