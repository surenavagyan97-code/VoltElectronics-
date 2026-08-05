using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(ICatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery(Name = "categoryIds")] int[]? categoryIds = null,
        [FromQuery(Name = "priceBands")] string[]? priceBands = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null)
        => Ok(await catalog.GetProductsAsync(new ProductQuery(page, pageSize, categoryIds, priceBands, search, sort)));

    [HttpGet("featured")]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> Featured([FromQuery] int count = 4)
        => Ok(await catalog.GetFeaturedAsync(Math.Clamp(count, 1, 12)));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductDetailDto>> BySlug(string slug)
    {
        var product = await catalog.GetProductBySlugAsync(slug);
        return product is null ? NotFound() : Ok(product);
    }
}
