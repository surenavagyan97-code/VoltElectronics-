using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Application.Admin.Products;

/// <summary>
/// Upserts spreadsheet rows into the catalog, matching on SKU. Bad rows are reported, not fatal;
/// TransactionBehavior still wraps the whole import, so a crash rolls back everything.
/// </summary>
public sealed record ImportProductsCommand(IReadOnlyList<ImportProductRow> Rows)
    : ICommand<Result<ImportProductsResultDto>>;

internal sealed class ImportProductsHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : ICommandHandler<ImportProductsCommand, Result<ImportProductsResultDto>>
{
    public async Task<Result<ImportProductsResultDto>> HandleAsync(
        ImportProductsCommand command, CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var errors = new List<ImportRowError>();
        // Saving row by row keeps the SKU/slug uniqueness checks honest against earlier rows;
        // the command-level transaction still makes the import atomic on unexpected failure.
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in command.Rows)
        {
            var (error, wasUpdate) = await ImportRowAsync(row, seenSkus, cancellationToken);
            if (error is not null) errors.Add(new ImportRowError(row.RowNumber, error));
            else if (wasUpdate) updated++;
            else created++;
        }

        return new ImportProductsResultDto(created, updated, errors);
    }

    private async Task<(string? Error, bool WasUpdate)> ImportRowAsync(
        ImportProductRow row, HashSet<string> seenSkus, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(row.Name)) return ("Name is required.", false);
        if (string.IsNullOrWhiteSpace(row.Sku)) return ("SKU is required.", false);
        if (string.IsNullOrWhiteSpace(row.Category)) return ("Category is required.", false);
        if (row.Price is null or <= 0) return ("Price must be a positive number.", false);
        if (row.Stock is < 0) return ("Stock cannot be negative.", false);
        if (row.Rating is < 0 or > 5) return ("Rating must be between 0 and 5.", false);
        if (row.ReviewCount is < 0) return ("Review count cannot be negative.", false);

        var sku = row.Sku.Trim();
        if (!seenSkus.Add(sku)) return ("Duplicate SKU earlier in the file.", false);

        var category = await ResolveCategoryAsync(row.Category, ct);
        var status = ProductAuthoring.ParseStatus(row.Status);

        var product = await products.GetAggregateBySkuAsync(sku, ct);
        var wasUpdate = product is not null;

        if (product is null)
        {
            product = Product.Create(
                row.Name, Slug.From(row.Name), sku, category.Id, row.Description,
                row.Price.Value, row.CompareAtPrice, row.Stock ?? 0, status, row.Badge);
            product.ChangeSlug(await products.UniqueSlugAsync(row.Name, null, ct));
            products.Add(product);
        }
        else
        {
            var renamed = product.NeedsNewSlug(row.Name);
            product.Describe(
                row.Name, sku, category.Id, row.Description,
                row.Price.Value, row.CompareAtPrice, row.Stock ?? 0, status, row.Badge);
            if (renamed)
                product.ChangeSlug(await products.UniqueSlugAsync(row.Name, product.Id, ct));
        }

        if (row.Rating is not null || row.ReviewCount is not null)
            product.SetRating(row.Rating ?? product.Rating, row.ReviewCount ?? product.ReviewCount);

        if (row.Specs is not null)
            product.ReplaceSpecs(ParseSpecs(row.Specs));

        await unitOfWork.SaveChangesAsync(ct);
        return (null, wasUpdate);
    }

    /// <summary>Unknown category names become new categories, so a spreadsheet can seed a whole catalog.</summary>
    private async Task<Category> ResolveCategoryAsync(string name, CancellationToken ct)
    {
        var trimmed = name.Trim();
        var category = await categories.GetByNameAsync(trimmed, ct);
        if (category is not null) return category;

        category = Category.Create(trimmed);
        categories.Add(category);
        // Saved immediately so the new category has an id for the product FK and is
        // findable by the next row that names it.
        await unitOfWork.SaveChangesAsync(ct);
        return category;
    }

    /// <summary>Inverse of the export format: one "Name: Value" spec per line within the cell.</summary>
    private static IEnumerable<(string Name, string Value)> ParseSpecs(string specs) =>
        specs.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => (parts[0].Trim(), parts[1].Trim()));
}
