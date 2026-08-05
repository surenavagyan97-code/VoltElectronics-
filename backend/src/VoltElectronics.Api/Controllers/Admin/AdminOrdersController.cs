using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Common;
using VoltElectronics.Application.Orders;

namespace VoltElectronics.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/orders")]
public class AdminOrdersController(IAdminService admin, IOrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOrderListItemDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] string? search = null)
        => Ok(await admin.GetOrdersAsync(page, pageSize, status, search));

    [HttpGet("stats")]
    public async Task<ActionResult<AdminOrderStatsDto>> Stats()
        => Ok(await admin.GetOrderStatsAsync());

    [HttpGet("{orderNumber}")]
    public async Task<ActionResult<OrderDetailDto>> Get(string orderNumber)
    {
        // Admin bypasses the owner check by design — reuse the detail projection.
        var order = await orders.GetOrderAsync(orderNumber, userId: null, email: null, bypassOwnerCheck: true);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{orderNumber}/status")]
    public async Task<IActionResult> UpdateStatus(string orderNumber, UpdateOrderStatusRequest request)
    {
        var result = await admin.UpdateOrderStatusAsync(orderNumber, request.Status);
        return result.Success ? NoContent() : BadRequest(new { error = result.Error });
    }
}
