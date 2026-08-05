using VoltElectronics.Application.Payments;

namespace VoltElectronics.Infrastructure.Payments;

/// <summary>
/// Local development gateway with the same redirect flow as Ameriabank vPOS but no
/// external calls: the "pay page" is served by our own API (PaymentsController) and lets
/// you choose success or failure. Enabled via Payments:Provider = "Fake".
/// </summary>
public class FakePaymentProvider : IPaymentProvider
{
    public string Name => "Fake";

    public Task<PaymentInitResult> InitPaymentAsync(PaymentInitRequest request, CancellationToken ct = default)
    {
        var paymentId = Guid.NewGuid().ToString("N");
        // Host the fake pay page on the same origin the callback lives on.
        var callbackUri = new Uri(request.CallbackUrl);
        var payUrl = $"{callbackUri.GetLeftPart(UriPartial.Authority)}/api/payments/fake/pay" +
                     $"?paymentId={paymentId}" +
                     $"&orderNumber={Uri.EscapeDataString(request.OrderNumber)}" +
                     $"&amount={request.Amount:0.00}" +
                     $"&callback={Uri.EscapeDataString(request.CallbackUrl)}";
        return Task.FromResult(PaymentInitResult.Ok(paymentId, payUrl));
    }

    public Task<PaymentVerifyResult> VerifyCallbackAsync(IReadOnlyDictionary<string, string?> query, CancellationToken ct = default)
    {
        var paymentId = query.GetValueOrDefault("paymentID");
        var paid = query.GetValueOrDefault("result") == "success";
        return Task.FromResult(new PaymentVerifyResult(paymentId, paid,
            paid ? null : "Payment declined on the fake gateway."));
    }
}
