using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Categories;
using VoltElectronics.Application.Admin.Queries;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Identity;

namespace VoltElectronics.Api.Endpoints.Admin;

public static class AdminCategoryEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/api/admin/categories")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        categories.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
            Results.Ok(await dispatcher.Query(new AdminGetCategoriesQuery(), ct)));

        categories.MapPost("/", async (SaveCategoryRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.Ok(await dispatcher.Send(new CreateCategoryCommand(request), ct)));

        categories.MapPut("/{id:int}", async (int id, SaveCategoryRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new UpdateCategoryCommand(id, request), ct)));

        categories.MapDelete("/{id:int}", async (int id, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new DeleteCategoryCommand(id), ct)));

        // Same split as product images: the API decodes, resizes and writes the file, the command
        // only records the live URL. Categories carry a single card-sized tile image.
        categories.MapPost("/{id:int}/image", async (
                int id, IFormFile file, IDispatcher dispatcher, IWebHostEnvironment env, CancellationToken ct) =>
            {
                if (ImageUploads.Reject(file) is { } rejected) return rejected;

                using var source = await ImageUploads.TryLoadAsync(file, ct);
                if (source is null)
                    return Results.BadRequest(new { error = "The uploaded file isn't a readable image." });

                var uploads = ImageUploads.EnsureUploadsDir(env);
                var url = await ImageUploads.SaveVariantAsync(
                    source, uploads, Guid.NewGuid().ToString("N"), "category", ImageUploads.CardSize, ct);

                var result = await dispatcher.Send(new SetCategoryImageCommand(id, url), ct);
                return result.IsSuccess
                    ? Results.Ok(new { imageUrl = result.Value })
                    : ApiResults.Fail(result.Error!);
            })
            // JWT auth, no cookies — CSRF doesn't apply, and form binding demands a stance on it.
            .DisableAntiforgery();

        categories.MapDelete("/{id:int}/image", async (int id, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new RemoveCategoryImageCommand(id), ct)));
    }
}
