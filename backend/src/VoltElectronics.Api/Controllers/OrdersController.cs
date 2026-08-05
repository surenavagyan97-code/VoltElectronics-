using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Orders;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> List()
        => Ok(await orderService.GetOrdersAsync(User.GetUserId()!));

    /// <summary>Owner access; guests pass the email used at checkout as ?email=.</summary>
    [HttpGet("{orderNumber}")]
    public async Task<ActionResult<OrderDetailDto>> Get(string orderNumber, [FromQuery] string? email)
    {
        var order = await orderService.GetOrderAsync(orderNumber, User.GetUserId(), email);
        return order is null ? NotFound() : Ok(order);
    }
}
