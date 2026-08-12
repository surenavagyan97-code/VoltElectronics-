using System.Globalization;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Common.Abstractions;

namespace VoltElectronics.Infrastructure.Payments;

/// <summary>
/// Local development gateway with the same redirect flow as Ameriabank vPOS but no external
/// calls: the "pay page" is served by our own API and lets you choose success or failure.
/// Enabled via Payments:Provider = "Fake".
/// </summary>
public sealed class FakePaymentGateway(IOptions<PaymentsOptions> options) : IPaymentGateway
{
    public string Name => "Fake";

    public Task<PaymentInitResult> InitPaymentAsync(PaymentInitRequest request, CancellationToken cancellationToken = default)
    {
        var paymentId = Guid.NewGuid().ToString("N");
        var callbackUrl = options.Value.CallbackUrl();
        // Host the fake pay page on the same origin the callback lives on.
        var payUrl = $"{new Uri(callbackUrl).GetLeftPart(UriPartial.Authority)}/api/payments/fake/pay" +
                     $"?paymentId={paymentId}" +
                     $"&orderNumber={Uri.EscapeDataString(request.OrderNumber)}" +
                     $"&amount={request.Amount.ToString("0.00", CultureInfo.InvariantCulture)}" +
                     $"&currency={Uri.EscapeDataString(request.Currency)}" +
                     $"&callback={Uri.EscapeDataString(callbackUrl)}";
        return Task.FromResult(PaymentInitResult.Ok(paymentId, payUrl));
    }

    public Task<PaymentVerifyResult> VerifyCallbackAsync(IReadOnlyDictionary<string, string?> query, CancellationToken cancellationToken = default)
    {
        var paymentId = query.GetValueOrDefault("paymentID");
        var paid = query.GetValueOrDefault("result") == "success";
        return Task.FromResult(new PaymentVerifyResult(paymentId, paid,
            paid ? null : "Payment declined on the fake gateway."));
    }
}
