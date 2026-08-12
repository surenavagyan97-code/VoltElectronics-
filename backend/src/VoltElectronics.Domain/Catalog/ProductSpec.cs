namespace VoltElectronics.Domain.Catalog;

/// <summary>Part of the <see cref="Product"/> aggregate — only ever created through the root.</summary>
public sealed class ProductSpec
{
    private ProductSpec() { }

    internal ProductSpec(string name, string value, int sortOrder)
    {
        Name = name;
        Value = value;
        SortOrder = sortOrder;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public int SortOrder { get; private set; }
}
