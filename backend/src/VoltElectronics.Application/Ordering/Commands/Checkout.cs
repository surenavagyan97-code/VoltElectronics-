using Microsoft.Extensions.Logging;
using VoltElectronics.Application.Carts;
using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Application.Ordering.Commands;

public sealed record CheckoutCommand(CartKey CartKey, string? UserId, CheckoutRequest ShipTo)
    : ICommand<Result<CheckoutResponse>>;

/// <summary>
/// Re-prices the cart server-side, places a PendingPayment order and initializes the gateway payment.
/// Stock is <em>not</em> decremented here — that happens when payment is confirmed, so an abandoned
/// checkout never holds inventory hostage.
/// </summary>
internal sealed class CheckoutHandler(
    ICartRepository carts,
    IProductRepository products,
    IOrderRepository orders,
    IPaymentGateway gateway,
    ICurrencyConverter currency,
    IOrderNumberGenerator orderNumbers,
    IUnitOfWork unitOfWork,
    ILogger<CheckoutHandler> logger) : ICommandHandler<CheckoutCommand, Result<CheckoutResponse>>
{
    // The storefront lets shoppers browse/checkout in USD or EUR for convenience, but the
    // payment gateway only settles in AMD — every order is placed and charged in AMD regardless
    // of cart.Currency, which just drove the pre-payment display.
    private const string SettlementCurrency = "AMD";

    public async Task<Result<CheckoutResponse>> HandleAsync(CheckoutCommand command, CancellationToken cancellationToken)
    {
        // Shape rules (required fields, email format) are CheckoutValidator's job by the time
        // this runs; everything below needs the database.
        var shipTo = command.ShipTo;

        var cart = await carts.FindAsync(command.CartKey, cancellationToken);
        if (cart is null || cart.Items.Count == 0) return Error.Invalid("Your cart is empty.");

        var catalog = (await products.GetByIdsAsync(
                cart.Items.Select(i => i.ProductId).ToArray(), cancellationToken))
            .ToDictionary(p => p.Id);

        var lines = new List<OrderLine>(cart.Items.Count);
        var subtotalBase = 0m;
        foreach (var item in cart.Items)
        {
            if (!catalog.TryGetValue(item.ProductId, out var product))
                return Error.Invalid("An item in your cart is no longer available.");

            // Availability and stock are the product's own rules; a violation here is a 400.
            product.EnsureCanFulfil(item.Qty);

            subtotalBase += product.Price * item.Qty;
            lines.Add(new OrderLine(
                product.Id, product.Name,
                currency.Convert(product.Price, SettlementCurrency), item.Qty));
        }

        // Never trust client-side totals — shipping and tax are computed on the true base-currency
        // subtotal and only then converted, because converting each line first and re-summing can
        // drift a cent from per-line rounding.
        var (subtotal, shipping, tax, total) = PricingPolicy.Totals(subtotalBase);
        var totals = new OrderTotals(
            currency.Convert(subtotal, SettlementCurrency),
            currency.Convert(shipping, SettlementCurrency),
            currency.Convert(tax, SettlementCurrency),
            currency.Convert(total, SettlementCurrency),
            SettlementCurrency,
            currency.Rate(SettlementCurrency));

        var order = Order.Place(
            orderNumbers.Next(),
            command.UserId,
            shipTo.Email,
            ShippingAddress.Create(
                shipTo.FullName, shipTo.Company, shipTo.Street,
                shipTo.City, shipTo.State, shipTo.Zip, shipTo.Phone),
            totals,
            cart.Id,
            gateway.Name,
            lines);

        orders.Add(order);
        // Saved before initializing payment: the gateway is handed the order's real id.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var init = await gateway.InitPaymentAsync(new PaymentInitRequest(
            order.Id, order.OrderNumber, order.Totals.Total, order.Totals.Currency,
            $"Volt Electronics order {order.OrderNumber}"), cancellationToken);

        if (!init.Success)
        {
            order.AbandonUnpaid(init.Error);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Error.Invalid(init.Error ?? "Payment could not be initialized.");
        }

        order.AttachPayment(init.PaymentId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Checkout {OrderNumber}: {Total} via {Provider}, payment {PaymentId}",
            order.OrderNumber, order.Totals.Total, gateway.Name, init.PaymentId);

        return new CheckoutResponse(order.OrderNumber, init.RedirectUrl!);
    }
}
