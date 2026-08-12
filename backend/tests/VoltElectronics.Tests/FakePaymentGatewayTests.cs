using Microsoft.Extensions.Options;
using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Infrastructure.Payments;

namespace VoltElectronics.Tests;

public class FakePaymentGatewayTests
{
    // Default options point the callback at http://localhost:5002.
    private readonly FakePaymentGateway _gateway = new(Options.Create(new PaymentsOptions()));

    [Fact]
    public async Task Init_returns_pay_page_on_callback_origin_with_payment_id()
    {
        var result = await _gateway.InitPaymentAsync(new PaymentInitRequest(
            42, "ORD-123", 132.75m, "USD", "Volt order"));

        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.PaymentId));
        Assert.StartsWith("http://localhost:5002/api/payments/fake/pay?", result.RedirectUrl);
        Assert.Contains($"paymentId={result.PaymentId}", result.RedirectUrl);
        Assert.Contains("orderNumber=ORD-123", result.RedirectUrl);
    }

    [Fact]
    public async Task Verify_reports_paid_only_on_success_result()
    {
        var paid = await _gateway.VerifyCallbackAsync(new Dictionary<string, string?>
        {
            ["paymentID"] = "abc",
            ["result"] = "success",
        });
        var declined = await _gateway.VerifyCallbackAsync(new Dictionary<string, string?>
        {
            ["paymentID"] = "abc",
            ["result"] = "fail",
        });

        Assert.Equal(("abc", true), (paid.PaymentId, paid.IsPaid));
        Assert.False(declined.IsPaid);
        Assert.NotNull(declined.FailureReason);
    }
}
