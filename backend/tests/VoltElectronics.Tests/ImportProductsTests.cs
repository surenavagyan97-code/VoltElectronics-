using Microsoft.EntityFrameworkCore;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Categories;
using VoltElectronics.Application.Admin.Products;

namespace VoltElectronics.Tests;

public class ImportProductsTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static ImportProductRow Row(
        int rowNumber, string? name, string? sku, string? category, decimal? price,
        int? stock = 0, string? status = "Active", string? specs = null) =>
        new(rowNumber, name, sku, category, Description: null, price, CompareAtPrice: null,
            stock, status, Badge: null, Rating: null, ReviewCount: null, specs);

    [Fact]
    public async Task Import_creates_products_and_missing_categories()
    {
        var result = await _db.Dispatcher.Send(new ImportProductsCommand(
        [
            Row(2, "Laptop Pro", "IMP-1", "Computers", 1499m, stock: 5),
            Row(3, "Studio Mic", "IMP-2", "Audio", 199m, specs: "Pattern: Cardioid\nWeight: 550 g"),
        ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Created);
        Assert.Equal(0, result.Value.Updated);
        Assert.Empty(result.Value.Errors);

        using var fresh = _db.NewContext();
        var mic = fresh.Products.Include(p => p.Specs).Include(p => p.Category).Single(p => p.Sku == "IMP-2");
        Assert.Equal("Audio", mic.Category.Name);
        Assert.Equal("studio-mic", mic.Slug);
        Assert.Collection(mic.Specs,
            s => { Assert.Equal("Pattern", s.Name); Assert.Equal("Cardioid", s.Value); },
            s => { Assert.Equal("Weight", s.Name); Assert.Equal("550 g", s.Value); });
    }

    [Fact]
    public async Task Import_updates_existing_product_by_sku()
    {
        var existing = _db.AddProduct("Old Keyboard", 80m, 10);

        var result = await _db.Dispatcher.Send(new ImportProductsCommand(
            [Row(2, "New Keyboard", existing.Sku, "Test", 95m, stock: 3)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Created);
        Assert.Equal(1, result.Value.Updated);

        using var fresh = _db.NewContext();
        var updated = fresh.Products.Single(p => p.Sku == existing.Sku);
        Assert.Equal("New Keyboard", updated.Name);
        Assert.Equal(95m, updated.Price);
        Assert.Equal(3, updated.Stock);
        // Renamed via import → freshly resolved slug, same as the admin form.
        Assert.Equal("new-keyboard", updated.Slug);
    }

    [Fact]
    public async Task Bad_rows_are_reported_without_sinking_good_ones()
    {
        var result = await _db.Dispatcher.Send(new ImportProductsCommand(
        [
            Row(2, "Good Product", "IMP-OK", "Gear", 50m),
            Row(3, null, "IMP-NONAME", "Gear", 50m),
            Row(4, "No Price", "IMP-NOPRICE", "Gear", null),
            Row(5, "Dupe", "IMP-OK", "Gear", 60m),
        ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Created);
        Assert.Collection(result.Value.Errors,
            e => Assert.Equal(3, e.RowNumber),
            e => Assert.Equal(4, e.RowNumber),
            e => { Assert.Equal(5, e.RowNumber); Assert.Contains("Duplicate SKU", e.Error); });

        using var fresh = _db.NewContext();
        Assert.Single(fresh.Products.Where(p => p.Sku.StartsWith("IMP-")));
    }

    [Fact]
    public async Task Two_new_products_with_same_name_get_distinct_slugs()
    {
        var result = await _db.Dispatcher.Send(new ImportProductsCommand(
        [
            Row(2, "USB Cable", "IMP-A", "Cables", 10m),
            Row(3, "USB Cable", "IMP-B", "Cables", 12m),
        ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Created);

        using var fresh = _db.NewContext();
        var slugs = fresh.Products.Where(p => p.Name == "USB Cable").Select(p => p.Slug).ToList();
        Assert.Equal(2, slugs.Distinct().Count());
    }

    [Fact]
    public async Task Category_image_can_be_set_and_removed()
    {
        _db.AddProduct("Anything", 10m, 1); // seeds the "Test" category

        using (var fresh = _db.NewContext())
        {
            var id = fresh.Categories.Single().Id;

            var set = await _db.Dispatcher.Send(new SetCategoryImageCommand(id, "/uploads/cat.jpg"));
            Assert.True(set.IsSuccess);

            var removed = await _db.Dispatcher.Send(new RemoveCategoryImageCommand(id));
            Assert.True(removed.IsSuccess);
        }

        using var check = _db.NewContext();
        Assert.Null(check.Categories.Single().ImageUrl);
    }
}
