using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Catalog;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List()
        => Ok(await catalog.GetCategoriesAsync());
}
