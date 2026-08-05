using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Orders;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CheckoutResponse>> Checkout(CheckoutRequest request)
    {
        var key = HttpContext.GetCartKey();
        if (!key.IsValid)
            return BadRequest(new { error = $"Provide a {RequestIdentity.CartHeader} header or authenticate." });

        var result = await orderService.CheckoutAsync(key, User.GetUserId(), request);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}
