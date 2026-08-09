using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Cart;
using VoltElectronics.Application.Common;
using VoltElectronics.Application.Orders;
using VoltElectronics.Application.Payments;
using VoltElectronics.Domain.Entities;
using VoltElectronics.Domain.Enums;
using VoltElectronics.Infrastructure.Data;
using VoltElectronics.Infrastructure.Payments;

namespace VoltElectronics.Infrastructure.Orders;

public class OrderService(
    AppDbContext db,
    IPaymentProvider paymentProvider,
    IOptions<PaymentsOptions> paymentsOptions,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly PaymentsOptions _payments = paymentsOptions.Value;

    public async Task<CheckoutResult> CheckoutAsync(CartKey cartKey, string? userId, CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.State) || string.IsNullOrWhiteSpace(request.Zip))
            return CheckoutResult.Fail("Please fill in all required shipping fields.");

        var cart = await LoadCartAsync(cartKey);
        if (cart is null || cart.Items.Count == 0)
            return CheckoutResult.Fail("Your cart is empty.");

        foreach (var item in cart.Items)
        {
            if (item.Product.Status != ProductStatus.Active)
                return CheckoutResult.Fail($"\"{item.Product.Name}\" is no longer available.");
            if (item.Qty > item.Product.Stock)
                return CheckoutResult.Fail($"Only {item.Product.Stock} of \"{item.Product.Name}\" left in stock.");
        }

        // Never trust client-side totals — re-price from current DB prices.
        var (subtotal, shipping, tax, total) = Pricing.Totals(cart.Items.Sum(i => i.Product.Price * i.Qty));

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}",
            UserId = userId,
            GuestEmail = request.Email.Trim(),
            Status = OrderStatus.PendingPayment,
            ShipFullName = request.FullName.Trim(),
            ShipCompany = request.Company?.Trim(),
            ShipStreet = request.Street.Trim(),
            ShipCity = request.City.Trim(),
            ShipState = request.State.Trim(),
            ShipZip = request.Zip.Trim(),
            ShipPhone = request.Phone?.Trim(),
            Subtotal = subtotal,
            ShippingCost = shipping,
            Tax = tax,
            Total = total,
            CartId = cart.Id,
            PaymentProvider = paymentProvider.Name,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                UnitPrice = i.Product.Price,
                Qty = i.Qty
            }).ToList()
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var callbackUrl = $"{_payments.CallbackBaseUrl.TrimEnd('/')}/api/payments/callback";
        var init = await paymentProvider.InitPaymentAsync(new PaymentInitRequest(
            order.Id, order.OrderNumber, order.Total,
            $"Volt Electronics order {order.OrderNumber}", callbackUrl));

        if (!init.Success)
        {
            order.Status = OrderStatus.Cancelled;
            order.PaymentFailureReason = init.Error;
            await db.SaveChangesAsync();
            return CheckoutResult.Fail(init.Error ?? "Payment could not be initialized.");
        }

        order.PaymentId = init.PaymentId;
        await db.SaveChangesAsync();

        logger.LogInformation("Checkout {OrderNumber}: {Total} via {Provider}, payment {PaymentId}",
            order.OrderNumber, order.Total, paymentProvider.Name, init.PaymentId);
        return CheckoutResult.Ok(new CheckoutResponse(order.OrderNumber, init.RedirectUrl!));
    }

    public async Task<PaymentCallbackOutcome> HandleCallbackAsync(IReadOnlyDictionary<string, string?> query)
    {
        var verify = await paymentProvider.VerifyCallbackAsync(query);
        if (verify.PaymentId is null)
        {
            logger.LogWarning("Payment callback without a payment id: {Query}",
                string.Join("&", query.Select(kv => $"{kv.Key}={kv.Value}")));
            return new PaymentCallbackOutcome(null, false);
        }

        var order = await db.Orders.FirstOrDefaultAsync(o => o.PaymentId == verify.PaymentId);
        if (order is null)
        {
            logger.LogWarning("Payment callback for unknown payment {PaymentId}", verify.PaymentId);
            return new PaymentCallbackOutcome(null, false);
        }

        // Idempotent: a replayed/refreshed callback must not double-process.
        if (order.Status != OrderStatus.PendingPayment)
            return new PaymentCallbackOutcome(order.OrderNumber, order.PaidAt is not null);

        if (!verify.IsPaid)
        {
            order.PaymentFailureReason = verify.FailureReason;
            await db.SaveChangesAsync();
            logger.LogInformation("Payment failed for {OrderNumber}: {Reason}", order.OrderNumber, verify.FailureReason);
            return new PaymentCallbackOutcome(order.OrderNumber, false);
        }

        order.Status = OrderStatus.Processing;
        order.PaidAt = DateTime.UtcNow;
        order.PaymentFailureReason = null;

        var items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
        var productIds = items.Select(i => i.ProductId).ToArray();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        foreach (var item in items)
        {
            if (products.TryGetValue(item.ProductId, out var product))
                product.Stock = Math.Max(0, product.Stock - item.Qty);
        }

        if (order.CartId is Guid cartId)
        {
            var cartItems = db.CartItems.Where(ci => ci.CartId == cartId);
            db.CartItems.RemoveRange(cartItems);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Order {OrderNumber} paid and moved to Processing", order.OrderNumber);
        return new PaymentCallbackOutcome(order.OrderNumber, true);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(string userId) =>
        await db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.OrderNumber, o.Status.ToString(), o.CreatedAt, o.Total, o.Items.Sum(i => i.Qty)))
            .ToListAsync();

    public async Task<OrderDetailDto?> GetOrderAsync(string orderNumber, string? userId, string? email, bool bypassOwnerCheck = false)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product!).ThenInclude(p => p.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (order is null) return null;

        var isOwner = order.UserId is not null
            ? order.UserId == userId
            : email is not null && string.Equals(order.GuestEmail, email, StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !bypassOwnerCheck) return null;

        return new OrderDetailDto(
            order.OrderNumber, order.Status.ToString(), order.CreatedAt, order.PaidAt,
            order.PaymentFailureReason,
            order.ShipFullName, order.ShipCompany, order.ShipStreet, order.ShipCity,
            order.ShipState, order.ShipZip, order.ShipPhone,
            order.Subtotal, order.ShippingCost, order.Tax, order.Total,
            order.Items.Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, i.Product?.Slug,
                i.Product?.Images.OrderBy(img => img.SortOrder).Select(img => img.CardUrl).FirstOrDefault(),
                i.UnitPrice, i.Qty)).ToList());
    }

    private Task<Domain.Entities.Cart?> LoadCartAsync(CartKey key)
    {
        var q = db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).AsSplitQuery();
        return key.UserId is not null
            ? q.FirstOrDefaultAsync(c => c.UserId == key.UserId)
            : q.FirstOrDefaultAsync(c => c.Id == key.GuestId && c.UserId == null);
    }
}
