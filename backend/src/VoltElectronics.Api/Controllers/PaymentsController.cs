using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Orders;
using VoltElectronics.Application.Payments;
using VoltElectronics.Infrastructure.Payments;

namespace VoltElectronics.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    IOrderService orderService,
    IPaymentProvider paymentProvider,
    IOptions<PaymentsOptions> paymentsOptions) : ControllerBase
{
    private readonly PaymentsOptions _payments = paymentsOptions.Value;

    /// <summary>
    /// The gateway (Ameriabank vPOS or the fake dev gateway) redirects the shopper here after
    /// the payment attempt. The result is verified server-side, then the shopper is sent to the
    /// storefront confirmation page.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var query = Request.Query.ToDictionary(kv => kv.Key, kv => (string?)kv.Value.ToString());
        var outcome = await orderService.HandleCallbackAsync(query);

        var fe = _payments.FrontendBaseUrl.TrimEnd('/');
        return Redirect(outcome.OrderNumber is null
            ? $"{fe}/?paymentError=1"
            : $"{fe}/confirmation/{Uri.EscapeDataString(outcome.OrderNumber)}?paid={(outcome.Paid ? "1" : "0")}");
    }

    /// <summary>Fake gateway pay page for local dev (Payments:Provider = "Fake"). Mimics the bank's hosted page.</summary>
    [HttpGet("fake/pay")]
    public IActionResult FakePayPage(
        [FromQuery] string paymentId, [FromQuery] string orderNumber,
        [FromQuery] string amount, [FromQuery] string callback, [FromQuery] string currency = "USD")
    {
        if (paymentProvider.Name != "Fake") return NotFound();
        // Only ever bounce back to our own callback endpoint.
        if (!callback.StartsWith($"{_payments.CallbackBaseUrl.TrimEnd('/')}/api/payments/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Invalid callback URL.");

        string Link(string result) =>
            $"{callback}?paymentID={Uri.EscapeDataString(paymentId)}&result={result}";

        var html = $$"""
            <!doctype html>
            <html><head><meta charset="utf-8"><title>Volt Dev Gateway</title>
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <style>
              body { font-family: system-ui, sans-serif; background: #0f1115; color: #e8eaf0;
                     display: grid; place-items: center; min-height: 100vh; margin: 0; }
              .card { background: #171a21; border: 1px solid #2a2f3a; border-radius: 12px;
                      padding: 32px 36px; max-width: 380px; width: 90%; }
              h1 { font-size: 18px; margin: 0 0 4px; } p { color: #9aa3b2; margin: 4px 0 20px; }
              .amount { font-size: 32px; font-weight: 700; margin-bottom: 24px; }
              a { display: block; text-align: center; padding: 12px; border-radius: 8px;
                  text-decoration: none; font-weight: 600; margin-bottom: 10px; }
              .pay { background: #4f7cff; color: white; } .fail { background: transparent;
                  color: #ff6b6b; border: 1px solid #3a2a2a; }
            </style></head>
            <body><div class="card">
              <h1>Fake payment gateway</h1>
              <p>Order {{WebUtility.HtmlEncode(orderNumber)}} &middot; dev only — no real charge</p>
              <div class="amount">{{WebUtility.HtmlEncode(amount)}} {{WebUtility.HtmlEncode(currency)}}</div>
              <a class="pay" href="{{WebUtility.HtmlEncode(Link("success"))}}">Pay now</a>
              <a class="fail" href="{{WebUtility.HtmlEncode(Link("fail"))}}">Simulate declined card</a>
            </div></body></html>
            """;
        return Content(html, "text/html");
    }
}
