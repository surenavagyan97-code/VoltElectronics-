using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Couriers;
using VoltElectronics.Application.Admin.Queries;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Identity;

namespace VoltElectronics.Api.Endpoints.Admin;

public static class AdminCourierEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var couriers = app.MapGroup("/api/admin/couriers")
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        couriers.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
            Results.Ok(await dispatcher.Query(new AdminGetCouriersQuery(), ct)));

        couriers.MapPost("/", async (CreateCourierRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.Ok(await dispatcher.Send(
                new CreateCourierCommand(request.Email, request.Password, request.FullName), ct)));

        couriers.MapDelete("/{id}", async (string id, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(new DeleteCourierCommand(id), ct)));
    }
}
