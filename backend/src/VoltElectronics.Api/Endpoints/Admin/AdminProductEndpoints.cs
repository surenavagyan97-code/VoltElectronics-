using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Products;
using VoltElectronics.Application.Admin.Queries;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Identity;

namespace VoltElectronics.Api.Endpoints.Admin;

public static class AdminProductEndpoints
{
    private const long MaxImportBytes = 10 * 1024 * 1024;

    public static void Map(IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/admin/products")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        products.MapGet("/", async (
                IDispatcher dispatcher, CancellationToken ct,
                int page = 1, int pageSize = 20, string? search = null) =>
            Results.Ok(await dispatcher.Query(new AdminGetProductsQuery(page, pageSize, search), ct)));

        // Excel round-trip: the exported sheet is also the import template. Rows are matched to
        // existing products by SKU — matched rows update, new SKUs create, bad rows are reported
        // per row without sinking the rest of the file.
        products.MapGet("/export", async (IDispatcher dispatcher, CancellationToken ct) =>
        {
            var rows = await dispatcher.Query(new ExportProductsQuery(), ct);
            return Results.File(
                ProductsWorkbook.Build(rows),
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"products-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
        });

        // A blank, annotated version of the same sheet for admins to prefill by hand.
        products.MapGet("/import/template", async (IDispatcher dispatcher, CancellationToken ct) =>
        {
            var categories = await dispatcher.Query(new AdminGetCategoriesQuery(), ct);
            return Results.File(
                ProductsWorkbook.BuildTemplate(categories),
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: "products-import-template.xlsx");
        });

        products.MapPost("/import", async (IFormFile file, IDispatcher dispatcher, CancellationToken ct) =>
            {
                if (file.Length == 0) return Results.BadRequest(new { error = "Empty file." });
                if (file.Length > MaxImportBytes)
                    return Results.BadRequest(new { error = "Import file must be 10 MB or smaller." });
                if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { error = "Only .xlsx files are allowed." });

                List<ImportProductRow> rows;
                List<ImportRowError> parseErrors;
                try
                {
                    await using var input = file.OpenReadStream();
                    (rows, parseErrors) = ProductsWorkbook.Parse(input);
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { error = "The uploaded file isn't a readable Excel workbook." });
                }

                var result = await dispatcher.Send(new ImportProductsCommand(rows), ct);
                if (!result.IsSuccess) return ApiResults.Fail(result.Error!);

                var import = result.Value!;
                return Results.Ok(new ImportProductsResultDto(
                    import.Created, import.Updated,
                    parseErrors.Concat(import.Errors).OrderBy(e => e.RowNumber).ToList()));
            })
            // JWT auth, no cookies — CSRF doesn't apply, and form binding demands a stance on it.
            .DisableAntiforgery();

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
                if (ImageUploads.Reject(file) is { } rejected) return rejected;

                using var source = await ImageUploads.TryLoadAsync(file, ct);
                if (source is null)
                    return Results.BadRequest(new { error = "The uploaded file isn't a readable image." });

                var uploads = ImageUploads.EnsureUploadsDir(env);
                var baseName = Guid.NewGuid().ToString("N");

                var thumbUrl = await ImageUploads.SaveVariantAsync(source, uploads, baseName, "thumb", ImageUploads.ThumbSize, ct);
                var cardUrl = await ImageUploads.SaveVariantAsync(source, uploads, baseName, "card", ImageUploads.CardSize, ct);
                var detailUrl = await ImageUploads.SaveVariantAsync(source, uploads, baseName, "detail", ImageUploads.DetailSize, ct);

                return ApiResults.Ok(await dispatcher.Send(
                    new AddProductImageCommand(id, detailUrl, thumbUrl, cardUrl), ct));
            })
            // JWT auth, no cookies — CSRF doesn't apply, and form binding demands a stance on it.
            .DisableAntiforgery();

        products.MapDelete("/{id:int}/images/{imageId:int}", async (
                int id, int imageId, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new RemoveProductImageCommand(id, imageId), ct)));
    }
}
