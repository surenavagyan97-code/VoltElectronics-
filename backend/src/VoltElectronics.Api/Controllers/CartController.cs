using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Cart;
using VoltElectronics.Infrastructure.Carts;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<CartDto>> Get() => Run(key => cartService.GetAsync(key));

    [HttpPost("items")]
    public Task<ActionResult<CartDto>> AddItem(AddCartItemRequest request)
        => Run(key => cartService.AddItemAsync(key, request.ProductId, request.Qty));

    [HttpPut("items/{productId:int}")]
    public Task<ActionResult<CartDto>> UpdateItem(int productId, UpdateCartItemRequest request)
        => Run(key => cartService.UpdateItemAsync(key, productId, request.Qty));

    [HttpDelete("items/{productId:int}")]
    public Task<ActionResult<CartDto>> RemoveItem(int productId)
        => Run(key => cartService.RemoveItemAsync(key, productId));

    [HttpDelete]
    public Task<ActionResult<CartDto>> Clear() => Run(key => cartService.ClearAsync(key));

    [HttpPut("currency")]
    public Task<ActionResult<CartDto>> SetCurrency(SetCartCurrencyRequest request)
        => Run(key => cartService.SetCurrencyAsync(key, request.Currency));

    /// <summary>Fold the pre-login guest cart into the authenticated user's cart.</summary>
    [Authorize]
    [HttpPost("merge")]
    public async Task<ActionResult<CartDto>> Merge()
    {
        var userId = User.GetUserId()!;
        return Guid.TryParse(Request.Headers[RequestIdentity.CartHeader], out var guestId)
            ? Ok(await cartService.MergeAsync(guestId, userId))
            : BadRequest(new { error = $"Missing {RequestIdentity.CartHeader} header." });
    }

    private async Task<ActionResult<CartDto>> Run(Func<CartKey, Task<CartDto>> action)
    {
        var key = HttpContext.GetCartKey();
        if (!key.IsValid)
            return BadRequest(new { error = $"Provide a {RequestIdentity.CartHeader} header or authenticate." });
        try
        {
            return Ok(await action(key));
        }
        catch (CartException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
