using VoltElectronics.Application.Cart;

namespace VoltElectronics.Application.Orders;

public interface IOrderService
{
    /// <summary>Re-prices the cart server-side, creates a PendingPayment order and initializes the gateway payment.</summary>
    Task<CheckoutResult> CheckoutAsync(CartKey cartKey, string? userId, CheckoutRequest request);

    /// <summary>Verifies a gateway redirect callback and finalizes the order (idempotent).</summary>
    Task<PaymentCallbackOutcome> HandleCallbackAsync(IReadOnlyDictionary<string, string?> query);

    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(string userId);

    /// <summary>Owner access: the order's user, or — for guest orders — anyone who knows the order number + email.
    /// bypassOwnerCheck is for admin endpoints only.</summary>
    Task<OrderDetailDto?> GetOrderAsync(string orderNumber, string? userId, string? email, bool bypassOwnerCheck = false);
}
