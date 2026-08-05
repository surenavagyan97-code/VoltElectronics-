using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common;

namespace VoltElectronics.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/products")]
public class AdminProductsController(IAdminService admin, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminProductListItemDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        => Ok(await admin.GetProductsAsync(page, pageSize, search));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminProductDetailDto>> Get(int id)
    {
        var product = await admin.GetProductAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveProductRequest request)
    {
        var (result, id) = await admin.CreateProductAsync(request);
        return result.Success ? Ok(new { id }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveProductRequest request)
    {
        var result = await admin.UpdateProductAsync(id, request);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await admin.DeleteProductAsync(id);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult<ProductImageDto>> UploadImage(int id, IFormFile file)
    {
        if (file.Length == 0) return BadRequest(new { error = "Empty file." });
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return BadRequest(new { error = $"Only {string.Join(", ", AllowedImageExtensions)} files are allowed." });

        var uploads = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploads);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(uploads, fileName)))
            await file.CopyToAsync(stream);

        var (result, image) = await admin.AddProductImageAsync(id, $"/uploads/{fileName}");
        return result.Success ? Ok(image) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> RemoveImage(int id, int imageId)
    {
        var result = await admin.RemoveProductImageAsync(id, imageId);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }
}
