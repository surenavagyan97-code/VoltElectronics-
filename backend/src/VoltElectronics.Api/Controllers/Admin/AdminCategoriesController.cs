using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Catalog;

namespace VoltElectronics.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/categories")]
public class AdminCategoriesController(IAdminService admin) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List()
        => Ok(await admin.GetCategoriesAsync());

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(SaveCategoryRequest request)
    {
        var (result, category) = await admin.CreateCategoryAsync(request);
        return result.Success ? Ok(category) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveCategoryRequest request)
    {
        var result = await admin.UpdateCategoryAsync(id, request);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await admin.DeleteCategoryAsync(id);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }
}
