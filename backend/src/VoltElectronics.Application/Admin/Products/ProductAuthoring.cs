using VoltElectronics.Application.Catalog;
using VoltElectronics.Domain.Catalog;

namespace VoltElectronics.Application.Admin.Products;

/// <summary>Shared write-side plumbing for the admin product form.</summary>
internal static class ProductAuthoring
{
    /// <summary>
    /// Slug uniqueness is a catalog-wide rule, so it can't live on the aggregate — it's resolved
    /// against the repository by appending -2, -3, … until the slug is free.
    /// </summary>
    public static async Task<string> UniqueSlugAsync(
        this IProductRepository products, string name, int? exceptProductId, CancellationToken ct)
    {
        var baseSlug = Slug.From(name);
        var slug = baseSlug;
        for (var i = 2; await products.SlugExistsAsync(slug, exceptProductId, ct); i++)
            slug = $"{baseSlug}-{i}";
        return slug;
    }

    /// <summary>An unrecognized status falls back to Active, matching the admin form's default.</summary>
    public static ProductStatus ParseStatus(string? status) =>
        Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var parsed) ? parsed : ProductStatus.Active;

    public static IEnumerable<(string Name, string Value)> ToSpecPairs(this List<ProductSpecDto>? specs) =>
        (specs ?? []).Select(s => (s.Name, s.Value));
}
