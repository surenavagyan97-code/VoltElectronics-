using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Products;
using VoltElectronics.Application.Admin.Queries;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Identity;

namespace VoltElectronics.Api.Endpoints.Admin;

public static class AdminProductEndpoints
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly JpegEncoder JpegEncoder = new() { Quality = 82 };

    // Longest-edge caps for the three variants every upload is resized into.
    private const int ThumbSize = 160;  // admin table + detail-page thumbnail strip
    private const int CardSize = 640;   // listing/featured cards, cart & order line items
    private const int DetailSize = 1600; // product detail main viewer

    public static void Map(IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/admin/products")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        products.MapGet("/", async (
                IDispatcher dispatcher, CancellationToken ct,
                int page = 1, int pageSize = 20, string? search = null) =>
            Results.Ok(await dispatcher.Query(new AdminGetProductsQuery(page, pageSize, search), ct)));

        products.MapGet("/{id:int}", async (int id, IDispatcher dispatcher, CancellationToken ct) =>
            await dispatcher.Query(new AdminGetProductQuery(id), ct) is { } product
                ? Results.Ok(product)
                : Results.NotFound());

        products.MapPost("/", async (SaveProductRequest request, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new CreateProductCommand(request), ct);
            return result.IsSuccess ? Results.Ok(new { id = result.Value }) : ApiResults.Fail(result.Error!);
        });

        products.MapPut("/{id:int}", async (int id, SaveProductRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new UpdateProductCommand(id, request), ct)));

        products.MapDelete("/{id:int}", async (int id, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new DeleteProductCommand(id), ct)));

        // The API owns the file side of an upload — decode, resize into the three variants, write
        // them under wwwroot — and only then records the URLs against the product.
        products.MapPost("/{id:int}/images", async (
                int id, IFormFile file, IDispatcher dispatcher, IWebHostEnvironment env, CancellationToken ct) =>
            {
                if (file.Length == 0) return Results.BadRequest(new { error = "Empty file." });
                if (file.Length > MaxImageBytes) return Results.BadRequest(new { error = "Image must be 5 MB or smaller." });
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(ext))
                    return Results.BadRequest(new { error = $"Only {string.Join(", ", AllowedImageExtensions)} files are allowed." });

                Image source;
                try
                {
                    await using var input = file.OpenReadStream();
                    source = await Image.LoadAsync(input, ct);
                }
                catch (UnknownImageFormatException)
                {
                    return Results.BadRequest(new { error = "The uploaded file isn't a readable image." });
                }

                using (source)
                {
                    var uploads = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
                    Directory.CreateDirectory(uploads);
                    var baseName = Guid.NewGuid().ToString("N");

                    var thumbUrl = await SaveVariantAsync(source, uploads, baseName, "thumb", ThumbSize, ct);
                    var cardUrl = await SaveVariantAsync(source, uploads, baseName, "card", CardSize, ct);
                    var detailUrl = await SaveVariantAsync(source, uploads, baseName, "detail", DetailSize, ct);

                    return ApiResults.Ok(await dispatcher.Send(
                        new AddProductImageCommand(id, detailUrl, thumbUrl, cardUrl), ct));
                }
            })
            // JWT auth, no cookies — CSRF doesn't apply, and form binding demands a stance on it.
            .DisableAntiforgery();

        products.MapDelete("/{id:int}/images/{imageId:int}", async (
                int id, int imageId, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new RemoveProductImageCommand(id, imageId), ct)));
    }

    /// <summary>Resizes to fit within size×size (no upscaling, aspect preserved) and saves as JPEG.</summary>
    private static async Task<string> SaveVariantAsync(
        Image source, string uploadsDir, string baseName, string suffix, int size, CancellationToken ct)
    {
        using var variant = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(size, size),
            Sampler = KnownResamplers.Lanczos3,
        }));
        var fileName = $"{baseName}-{suffix}.jpg";
        await using var stream = File.Create(Path.Combine(uploadsDir, fileName));
        await variant.SaveAsync(stream, JpegEncoder, ct);
        return $"/uploads/{fileName}";
    }
}
