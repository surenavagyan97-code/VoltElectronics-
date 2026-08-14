using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Catalog.Queries;
using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Catalog");

        // Every storefront read takes an optional ?lang=xx: display names resolve server-side to
        // that language's translation, falling back to the canonical name.
        products.MapGet("/", async (
                IDispatcher dispatcher, CancellationToken ct,
                int page = 1, int pageSize = 12, int[]? categoryIds = null,
                string[]? priceBands = null, string? search = null, string? sort = null, string? lang = null) =>
            Results.Ok(await dispatcher.Query(
                new GetProductsQuery(page, pageSize, categoryIds, priceBands, search, sort, lang), ct)));

        products.MapGet("/featured", async (IDispatcher dispatcher, CancellationToken ct, int count = 4, string? lang = null) =>
            Results.Ok(await dispatcher.Query(new GetFeaturedProductsQuery(count, lang), ct)));

        // The favorites page: ids live in the shopper's browser, so it asks for exactly those.
        // Literal segment, so it wins over the {slug} route below.
        products.MapGet("/by-ids", async (int[] ids, IDispatcher dispatcher, CancellationToken ct, string? lang = null) =>
        {
            var items = ids.Length == 0
                ? Array.Empty<ProductListItemDto>()
                : await dispatcher.Query(new GetProductsByIdsQuery(ids.Take(100).ToArray(), lang), ct);
            return Results.Ok(items);
        });

        products.MapGet("/{slug}", async (string slug, IDispatcher dispatcher, CancellationToken ct, string? lang = null) =>
            await dispatcher.Query(new GetProductBySlugQuery(slug, lang), ct) is { } product
                ? Results.Ok(product)
                : Results.NotFound());

        app.MapGet("/api/categories", async (IDispatcher dispatcher, CancellationToken ct) =>
                Results.Ok(await dispatcher.Query(new GetCategoriesQuery(), ct)))
            .WithTags("Catalog");
    }
}
