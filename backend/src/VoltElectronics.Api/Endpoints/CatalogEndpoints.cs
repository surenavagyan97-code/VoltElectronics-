using VoltElectronics.Application.Catalog.Queries;
using VoltElectronics.Application.Common.Messaging;

namespace VoltElectronics.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Catalog");

        products.MapGet("/", async (
                IDispatcher dispatcher, CancellationToken ct,
                int page = 1, int pageSize = 12, int[]? categoryIds = null,
                string[]? priceBands = null, string? search = null, string? sort = null) =>
            Results.Ok(await dispatcher.Query(
                new GetProductsQuery(page, pageSize, categoryIds, priceBands, search, sort), ct)));

        products.MapGet("/featured", async (IDispatcher dispatcher, CancellationToken ct, int count = 4) =>
            Results.Ok(await dispatcher.Query(new GetFeaturedProductsQuery(count), ct)));

        products.MapGet("/{slug}", async (string slug, IDispatcher dispatcher, CancellationToken ct) =>
            await dispatcher.Query(new GetProductBySlugQuery(slug), ct) is { } product
                ? Results.Ok(product)
                : Results.NotFound());

        app.MapGet("/api/categories", async (IDispatcher dispatcher, CancellationToken ct) =>
                Results.Ok(await dispatcher.Query(new GetCategoriesQuery(), ct)))
            .WithTags("Catalog");
    }
}
