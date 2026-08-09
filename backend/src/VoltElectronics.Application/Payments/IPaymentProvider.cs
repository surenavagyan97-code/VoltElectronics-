namespace VoltElectronics.Application.Payments;

public record PaymentInitRequest(
    int OrderId, string OrderNumber, decimal Amount, string Currency, string Description,
    /// <summary>Absolute URL the gateway redirects the shopper back to after the payment attempt.</summary>
    string CallbackUrl);

public record PaymentInitResult(bool Success, string? PaymentId, string? RedirectUrl, string? Error)
{
    public static PaymentInitResult Ok(string paymentId, string redirectUrl) => new(true, paymentId, redirectUrl, null);
    public static PaymentInitResult Fail(string error) => new(false, null, null, error);
}

public record PaymentVerifyResult(string? PaymentId, bool IsPaid, string? FailureReason);

/// <summary>
/// Redirect-style payment gateway (Ameriabank vPOS in production; a local fake for dev).
/// Flow: InitPaymentAsync → send shopper to RedirectUrl → gateway redirects back to
/// CallbackUrl → VerifyCallbackAsync confirms the result server-side (never trust query params alone).
/// </summary>
public interface IPaymentProvider
{
    string Name { get; }
    Task<PaymentInitResult> InitPaymentAsync(PaymentInitRequest request, CancellationToken ct = default);
    Task<PaymentVerifyResult> VerifyCallbackAsync(IReadOnlyDictionary<string, string?> query, CancellationToken ct = default);
}
