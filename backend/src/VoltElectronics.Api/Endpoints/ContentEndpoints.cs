using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Content;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Content.Queries;
using VoltElectronics.Application.Identity;

namespace VoltElectronics.Api.Endpoints;

/// <summary>Admin-editable storefront pages: the public read and the admin write.</summary>
public static class ContentEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/content/{key}", async (string key, IDispatcher dispatcher, CancellationToken ct) =>
                await dispatcher.Query(new GetContentPageQuery(key), ct) is { } page
                    ? Results.Ok(page)
                    : Results.NotFound())
            .WithTags("Content");

        app.MapPut("/api/admin/content/{key}", async (
                string key, SaveContentRequest request, IDispatcher dispatcher, CancellationToken ct) =>
                ApiResults.NoContent(await dispatcher.Send(new UpdateContentPageCommand(key, request.Body), ct)))
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
    }
}
