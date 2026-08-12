namespace VoltElectronics.Application.Common.Abstractions;

public record PaymentInitRequest(
    int OrderId, string OrderNumber, decimal Amount, string Currency, string Description);

public record PaymentInitResult(bool Success, string? PaymentId, string? RedirectUrl, string? Error)
{
    public static PaymentInitResult Ok(string paymentId, string redirectUrl) => new(true, paymentId, redirectUrl, null);
    public static PaymentInitResult Fail(string error) => new(false, null, null, error);
}

public record PaymentVerifyResult(string? PaymentId, bool IsPaid, string? FailureReason);

/// <summary>
/// Redirect-style payment gateway (Ameriabank vPOS in production; a local fake for dev).
/// Flow: InitPaymentAsync → send shopper to RedirectUrl → gateway redirects back to the gateway's
/// own callback URL → VerifyCallbackAsync confirms the result server-side (never trust query
/// params alone). The callback URL is the adapter's business, not the caller's — which is why it
/// isn't part of <see cref="PaymentInitRequest"/>.
/// </summary>
public interface IPaymentGateway
{
    string Name { get; }
    Task<PaymentInitResult> InitPaymentAsync(PaymentInitRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVerifyResult> VerifyCallbackAsync(IReadOnlyDictionary<string, string?> query, CancellationToken cancellationToken = default);
}
