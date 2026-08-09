using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Payments;

namespace VoltElectronics.Infrastructure.Payments;

/// <summary>
/// Ameriabank vPOS (Armenia) REST integration.
/// InitPayment → redirect shopper to the hosted pay page → bank redirects back to our
/// callback with ?paymentID=… → GetPaymentDetails confirms the charge server-side.
/// Docs: https://servicestest.ameriabank.am/VPOS (test) — ResponseCode "00" + OrderStatus 2
/// (deposited) means the money is captured.
/// </summary>
public class AmeriaVposProvider(HttpClient http, IOptions<PaymentsOptions> options, ILogger<AmeriaVposProvider> logger)
    : IPaymentProvider
{
    private readonly AmeriaVposOptions _opts = options.Value.Ameria;

    public string Name => "Ameria";

    public async Task<PaymentInitResult> InitPaymentAsync(PaymentInitRequest request, CancellationToken ct = default)
    {
        // Charge in whatever currency the order was placed in. Note: Ameriabank provisions a
        // merchant account for specific settlement currencies — confirm with the bank that
        // this ClientID actually accepts AMD/USD/EUR before relying on this in production.
        var body = new
        {
            ClientID = _opts.ClientId,
            Username = _opts.Username,
            Password = _opts.Password,
            OrderID = _opts.OrderIdOffset + request.OrderId,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            BackURL = request.CallbackUrl,
            Opaque = request.OrderNumber
        };

        InitPaymentResponse? res;
        try
        {
            var httpRes = await http.PostAsJsonAsync($"{_opts.BaseUrl}/api/VPOS/InitPayment", body, ct);
            httpRes.EnsureSuccessStatusCode();
            res = await httpRes.Content.ReadFromJsonAsync<InitPaymentResponse>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ameria InitPayment call failed for order {OrderNumber}", request.OrderNumber);
            return PaymentInitResult.Fail("Payment gateway is unavailable. Please try again.");
        }

        if (res is null || res.ResponseCode != 1 || string.IsNullOrEmpty(res.PaymentId))
        {
            logger.LogWarning("Ameria InitPayment rejected order {OrderNumber}: {Code} {Message}",
                request.OrderNumber, res?.ResponseCode, res?.ResponseMessage);
            return PaymentInitResult.Fail(res?.ResponseMessage ?? "Payment could not be initialized.");
        }

        var payUrl = $"{_opts.BaseUrl}/Payments/Pay?id={Uri.EscapeDataString(res.PaymentId)}&lang={_opts.Language}";
        return PaymentInitResult.Ok(res.PaymentId, payUrl);
    }

    public async Task<PaymentVerifyResult> VerifyCallbackAsync(IReadOnlyDictionary<string, string?> query, CancellationToken ct = default)
    {
        // The bank appends paymentID to BackURL; never trust the result code in the query — re-query the API.
        var paymentId = query.FirstOrDefault(kv => kv.Key.Equals("paymentID", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrEmpty(paymentId))
            return new PaymentVerifyResult(null, false, "Missing paymentID in gateway callback.");

        PaymentDetailsResponse? details;
        try
        {
            var httpRes = await http.PostAsJsonAsync($"{_opts.BaseUrl}/api/VPOS/GetPaymentDetails",
                new { PaymentID = paymentId, Username = _opts.Username, Password = _opts.Password }, ct);
            httpRes.EnsureSuccessStatusCode();
            details = await httpRes.Content.ReadFromJsonAsync<PaymentDetailsResponse>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ameria GetPaymentDetails call failed for payment {PaymentId}", paymentId);
            return new PaymentVerifyResult(paymentId, false, "Could not verify payment with the gateway.");
        }

        // OrderStatus 2 = payment_deposited (funds captured).
        var paid = details is { ResponseCode: "00", OrderStatus: 2 };
        var reason = paid ? null
            : $"Gateway response {details?.ResponseCode ?? "?"} ({details?.PaymentState ?? details?.ResponseMessage ?? "unknown state"})";
        return new PaymentVerifyResult(paymentId, paid, reason);
    }

    private sealed class InitPaymentResponse
    {
        [JsonPropertyName("PaymentID")] public string? PaymentId { get; set; }
        public int ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
    }

    private sealed class PaymentDetailsResponse
    {
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? PaymentState { get; set; }
        public int OrderStatus { get; set; }
        public decimal Amount { get; set; }
        public string? Opaque { get; set; }
    }
}
