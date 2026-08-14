using VoltElectronics.Application.Admin.Content;
using VoltElectronics.Application.Catalog.Queries;
using VoltElectronics.Application.Content.Queries;

namespace VoltElectronics.Tests;

/// <summary>
/// The translation-table pattern end to end: per-language rows resolve server-side and fall
/// back to the canonical/default language when a translation is missing.
/// </summary>
public class LocalizationTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Product_names_resolve_per_language_and_fall_back()
    {
        var product = _db.AddProduct("Laptop", 1000m, 5);
        product.ReplaceTranslations([("hy", "Նոթբուք")]);
        _db.Context.SaveChanges();

        var hy = await _db.Dispatcher.Query(new GetProductsQuery(Lang: "hy"));
        Assert.Equal("Նոթբուք", hy.Items.Single().Name);

        // No Russian translation → canonical name.
        var ru = await _db.Dispatcher.Query(new GetProductsQuery(Lang: "ru"));
        Assert.Equal("Laptop", ru.Items.Single().Name);
    }

    [Fact]
    public async Task Search_matches_translated_names()
    {
        var product = _db.AddProduct("Laptop", 1000m, 5);
        product.ReplaceTranslations([("hy", "Նոթբուք")]);
        _db.Context.SaveChanges();

        var result = await _db.Dispatcher.Query(new GetProductsQuery(Search: "Նոթբ", Lang: "hy"));
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Content_pages_fall_back_to_default_language()
    {
        Assert.True((await _db.Dispatcher.Send(new UpdateContentPageCommand("about", "en", "About EN"))).IsSuccess);
        Assert.True((await _db.Dispatcher.Send(new UpdateContentPageCommand("about", "hy", "About HY"))).IsSuccess);

        var hy = await _db.Dispatcher.Query(new GetContentPageQuery("about", "hy"));
        Assert.Equal("About HY", hy!.Body);

        // Missing translation falls back for the storefront…
        var ru = await _db.Dispatcher.Query(new GetContentPageQuery("about", "ru"));
        Assert.Equal("About EN", ru!.Body);
        Assert.Equal("en", ru.Lang);

        // …but not for the admin editor, which needs to see "not written yet".
        var ruExact = await _db.Dispatcher.Query(new GetContentPageQuery("about", "ru", Fallback: false));
        Assert.Null(ruExact);
    }
}
