using VoltElectronics.Domain.Common;

namespace VoltElectronics.Domain.Catalog;

public sealed class Category : AggregateRoot
{
    private Category() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Category Create(string name)
    {
        var trimmed = Require(name);
        return new Category { Name = trimmed, Slug = Catalog.Slug.From(trimmed) };
    }

    public void Rename(string name)
    {
        var trimmed = Require(name);
        Name = trimmed;
        Slug = Catalog.Slug.From(trimmed);
    }

    private static string Require(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? throw new DomainException("Name is required.")
            : name.Trim();
}
