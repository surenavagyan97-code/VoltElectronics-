using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Configuration;

namespace VoltElectronics.Api.Endpoints;

public static class ConfigEndpoints
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/config", async (IDispatcher dispatcher, CancellationToken ct) =>
                Results.Ok(await dispatcher.Query(new GetStorefrontConfigQuery(), ct)))
            .WithTags("Config");
}
