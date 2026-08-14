namespace VoltElectronics.Domain.Catalog;

/// <summary>
/// One language's display name for a product — the per-entity translation-table pattern.
/// Reachable only through the aggregate; a missing row means "fall back to Product.Name".
/// </summary>
public sealed class ProductTranslation
{
    private ProductTranslation() { }

    internal ProductTranslation(string lang, string name)
    {
        Lang = lang;
        Name = name;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string Lang { get; private set; } = null!;
    public string Name { get; private set; } = null!;
}
