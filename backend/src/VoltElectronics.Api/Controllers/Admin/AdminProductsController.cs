using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
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
    private static readonly JpegEncoder JpegEncoder = new() { Quality = 82 };

    // Longest-edge caps for the three variants every upload is resized into.
    private const int ThumbSize = 160;  // admin table + detail-page thumbnail strip
    private const int CardSize = 640;   // listing/featured cards, cart & order line items
    private const int DetailSize = 1600; // product detail main viewer

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

        Image source;
        try
        {
            await using var input = file.OpenReadStream();
            source = await Image.LoadAsync(input);
        }
        catch (UnknownImageFormatException)
        {
            return BadRequest(new { error = "The uploaded file isn't a readable image." });
        }

        using (source)
        {
            var uploads = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var baseName = Guid.NewGuid().ToString("N");

            var thumbUrl = await SaveVariantAsync(source, uploads, baseName, "thumb", ThumbSize);
            var cardUrl = await SaveVariantAsync(source, uploads, baseName, "card", CardSize);
            var detailUrl = await SaveVariantAsync(source, uploads, baseName, "detail", DetailSize);

            var (result, image) = await admin.AddProductImageAsync(id, detailUrl, thumbUrl, cardUrl);
            return result.Success ? Ok(image) : BadRequest(new { error = result.Error });
        }
    }

    /// <summary>Resizes to fit within size×size (no upscaling, aspect preserved) and saves as JPEG.</summary>
    private static async Task<string> SaveVariantAsync(Image source, string uploadsDir, string baseName, string suffix, int size)
    {
        using var variant = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(size, size),
            Sampler = KnownResamplers.Lanczos3,
        }));
        var fileName = $"{baseName}-{suffix}.jpg";
        await using var stream = System.IO.File.Create(Path.Combine(uploadsDir, fileName));
        await variant.SaveAsync(stream, JpegEncoder);
        return $"/uploads/{fileName}";
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> RemoveImage(int id, int imageId)
    {
        var result = await admin.RemoveProductImageAsync(id, imageId);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }
}
