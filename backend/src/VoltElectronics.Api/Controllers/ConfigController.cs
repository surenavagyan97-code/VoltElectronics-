using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Payments;
using VoltElectronics.Infrastructure.Payments;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController(IPaymentProvider paymentProvider, IOptions<PaymentsOptions> paymentsOptions) : ControllerBase
{
    /// <summary>Runtime config the storefront needs — no build-time secrets in the frontend bundle.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        paymentProvider = paymentProvider.Name,
        currency = paymentsOptions.Value.Ameria.Currency
    });
}
