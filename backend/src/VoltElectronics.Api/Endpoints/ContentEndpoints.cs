using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Content;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Content.Queries;
using VoltElectronics.Application.Identity;
using VoltElectronics.Domain.Content;

namespace VoltElectronics.Api.Endpoints;

/// <summary>Admin-editable storefront pages: the public read and the admin write.</summary>
public static class ContentEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // ?lang=xx picks the translation; a missing one falls back to the default language unless
        // fallback=false (the admin editor's mode, which wants to see "not written yet" as a 404).
        app.MapGet("/api/content/{key}", async (
                string key, IDispatcher dispatcher, CancellationToken ct,
                string lang = ContentPage.DefaultLang, bool fallback = true) =>
                await dispatcher.Query(new GetContentPageQuery(key, lang, fallback), ct) is { } page
                    ? Results.Ok(page)
                    : Results.NotFound())
            .WithTags("Content");

        app.MapPut("/api/admin/content/{key}", async (
                string key, SaveContentRequest request, IDispatcher dispatcher, CancellationToken ct) =>
                ApiResults.NoContent(await dispatcher.Send(new UpdateContentPageCommand(key, request.Lang, request.Body), ct)))
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
    }
}
