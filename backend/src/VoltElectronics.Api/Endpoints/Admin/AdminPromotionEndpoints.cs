using VoltElectronics.Application.Admin.Promotions;
using VoltElectronics.Application.Admin.Queries;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Identity;
using VoltElectronics.Application.Promotions;

namespace VoltElectronics.Api.Endpoints.Admin;

public static class AdminPromotionEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var promotions = app.MapGroup("/api/admin/promotions")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        promotions.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
            Results.Ok(await dispatcher.Query(new AdminGetPromotionsQuery(), ct)));

        promotions.MapPost("/", async (SavePromotionRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.Ok(await dispatcher.Send(new CreatePromotionCommand(request), ct)));

        promotions.MapPut("/{id:int}", async (int id, SavePromotionRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new UpdatePromotionCommand(id, request), ct)));

        promotions.MapDelete("/{id:int}", async (int id, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new DeletePromotionCommand(id), ct)));
    }
}
