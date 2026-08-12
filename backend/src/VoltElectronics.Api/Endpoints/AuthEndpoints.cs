using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Identity.Commands;

namespace VoltElectronics.Api.Endpoints;

public static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        // The command records are the request bodies — see Identity/Commands/AuthCommands.cs.
        auth.MapPost("/register", async (RegisterCommand command, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.Ok(await dispatcher.Send(command, ct)));

        auth.MapPost("/login", async (LoginCommand command, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.Ok(await dispatcher.Send(command, ct)));

        auth.MapPost("/refresh", async (RefreshSessionCommand command, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.Ok(await dispatcher.Send(command, ct)));

        auth.MapPost("/logout", async (LogoutCommand command, IDispatcher dispatcher, CancellationToken ct) =>
            ApiResults.NoContent(await dispatcher.Send(command, ct)));
    }
}
