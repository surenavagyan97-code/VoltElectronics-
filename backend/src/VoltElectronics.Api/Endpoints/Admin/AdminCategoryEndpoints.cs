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
    }
}
