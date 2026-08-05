using System.Security.Claims;
using VoltElectronics.Application.Cart;

namespace VoltElectronics.Api;

public static class RequestIdentity
{
    public const string CartHeader = "X-Cart-Id";

    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

    /// <summary>Authenticated user's cart, or a guest cart identified by the client-generated X-Cart-Id GUID.</summary>
    public static CartKey GetCartKey(this HttpContext ctx)
    {
        var userId = ctx.User.GetUserId();
        if (userId is not null) return new CartKey(userId, null);
        return Guid.TryParse(ctx.Request.Headers[CartHeader], out var guestId)
            ? new CartKey(null, guestId)
            : new CartKey(null, null);
    }
}
