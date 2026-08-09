using Microsoft.AspNetCore.Mvc;
using VoltElectronics.Application.Common;
using VoltElectronics.Application.Payments;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController(IPaymentProvider paymentProvider, ICurrencyConverter currency) : ControllerBase
{
    /// <summary>Runtime config the storefront needs — no build-time secrets in the frontend bundle.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        paymentProvider = paymentProvider.Name,
        currency = currency.BaseCurrency,
        supportedCurrencies = currency.SupportedCurrencies,
        rates = currency.SupportedCurrencies.ToDictionary(c => c, c => currency.Rate(c))
    });
}
