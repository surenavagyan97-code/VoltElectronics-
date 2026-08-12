using VoltElectronics.Domain.Common;

namespace VoltElectronics.Api.Middleware;

/// <summary>
/// An aggregate refusing a request ("only 3 in stock", "item not in cart") is a client error, not
/// a server fault — surface every <see cref="DomainException"/> as a 400 with the storefront's
/// usual <c>{ error }</c> body instead of a 500.
/// </summary>
internal sealed class DomainExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
    }
}
