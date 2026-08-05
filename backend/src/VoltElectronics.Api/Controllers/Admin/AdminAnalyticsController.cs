using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Admin;

namespace VoltElectronics.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/analytics")]
public class AdminAnalyticsController(IAdminService admin) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AnalyticsDto>> Get() => Ok(await admin.GetAnalyticsAsync());
}
