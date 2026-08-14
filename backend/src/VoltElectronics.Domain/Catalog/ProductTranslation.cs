namespace VoltElectronics.Domain.Catalog;

/// <summary>
/// One language's display texts for a product — the per-entity translation-table pattern.
/// Reachable only through the aggregate. Each field falls back independently: a null name or
/// description means "use the canonical one from Product".
/// </summary>
public sealed class ProductTranslation
{
    private ProductTranslation() { }

    internal ProductTranslation(string lang, string? name, string? description)
    {
        Lang = lang;
        Name = name;
        Description = description;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string Lang { get; private set; } = null!;
    public string? Name { get; private set; }
    public string? Description { get; private set; }
}
