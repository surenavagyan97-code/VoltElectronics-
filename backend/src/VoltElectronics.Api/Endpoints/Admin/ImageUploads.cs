using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace VoltElectronics.Api.Endpoints.Admin;

/// <summary>
/// The file side of every admin image upload — validate, decode, resize, write under
/// wwwroot/uploads — shared by the product and category endpoints.
/// </summary>
internal static class ImageUploads
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly JpegEncoder JpegEncoder = new() { Quality = 82 };

    // Longest-edge caps for the variants uploads are resized into.
    public const int ThumbSize = 160;   // admin tables + detail-page thumbnail strip
    public const int CardSize = 640;    // listing/featured cards, cart & order line items, category tiles
    public const int DetailSize = 1600; // product detail main viewer

    /// <summary>The upload's rejection as a 400, or null when it passes validation.</summary>
    public static IResult? Reject(IFormFile file)
    {
        if (file.Length == 0) return Results.BadRequest(new { error = "Empty file." });
        if (file.Length > MaxBytes) return Results.BadRequest(new { error = "Image must be 5 MB or smaller." });
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return Results.BadRequest(new { error = $"Only {string.Join(", ", AllowedExtensions)} files are allowed." });
        return null;
    }

    /// <summary>Null when the bytes aren't a decodable image; the extension check alone can't tell.</summary>
    public static async Task<Image?> TryLoadAsync(IFormFile file, CancellationToken ct)
    {
        try
        {
            await using var input = file.OpenReadStream();
            return await Image.LoadAsync(input, ct);
        }
        catch (UnknownImageFormatException)
        {
            return null;
        }
    }

    public static string EnsureUploadsDir(IWebHostEnvironment env)
    {
        var uploads = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);
        return uploads;
    }

    /// <summary>Resizes to fit within size×size (no upscaling, aspect preserved) and saves as JPEG.</summary>
    public static async Task<string> SaveVariantAsync(
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
