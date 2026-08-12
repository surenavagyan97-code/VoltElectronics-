using VoltElectronics.Application.Carts;
using VoltElectronics.Application.Carts.Commands;
using VoltElectronics.Application.Ordering;
using VoltElectronics.Application.Ordering.Commands;
using VoltElectronics.Application.Ordering.Queries;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Common;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Tests;

public class CheckoutTests : IDisposable
{
    private readonly TestDb _db = new();

    private static readonly CheckoutRequest ValidRequest = new(
        "shopper@example.com", "Jordan Lee", null, "500 Market St", "Yerevan", "Yerevan", "0010", null);

    public void Dispose() => _db.Dispose();

    private async Task<CartKey> GuestCartWith(int productId, int qty)
    {
        var key = new CartKey(null, Guid.NewGuid());
        await _db.Dispatcher.Send(new AddCartItemCommand(key, productId, qty));
        return key;
    }

    private Task<Application.Common.Results.Result<CheckoutResponse>> Checkout(CartKey key) =>
        _db.Dispatcher.Send(new CheckoutCommand(key, null, ValidRequest));

    [Fact]
    public async Task Checkout_fails_on_empty_cart()
    {
        var result = await Checkout(new CartKey(null, Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checkout_fails_when_stock_dropped_below_cart_quantity()
    {
        var product = _db.AddProduct("Console", 500m, 5);
        var key = await GuestCartWith(product.Id, 4);

        product.ReduceStock(3); // someone else bought the rest → 2 left
        await _db.Context.SaveChangesAsync();

        // Fulfilment rules are the Product aggregate's own; the API surfaces this as a 400.
        var ex = await Assert.ThrowsAsync<DomainException>(() => Checkout(key));
        Assert.Contains("left in stock", ex.Message);
    }

    [Fact]
    public async Task Checkout_reprices_server_side_and_creates_pending_order_with_payment()
    {
        var product = _db.AddProduct("Laptop", 1000m, 10);
        var key = await GuestCartWith(product.Id, 2);

        var result = await Checkout(key);

        Assert.True(result.IsSuccess);
        Assert.Contains("/api/payments/fake/pay", result.Value!.PaymentUrl);

        using var fresh = _db.NewContext();
        var order = fresh.Orders.Single();
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(2000m, order.Totals.Subtotal);
        Assert.Equal(2000m + 24m + Math.Round(2000m * 0.0875m, 2), order.Totals.Total);
        Assert.Equal("Fake", order.PaymentProvider);
        Assert.False(string.IsNullOrEmpty(order.PaymentId));
    }

    [Fact]
    public async Task Successful_callback_marks_paid_decrements_stock_and_clears_cart()
    {
        var product = _db.AddProduct("Camera", 1100m, 10);
        var key = await GuestCartWith(product.Id, 3);
        var checkout = await Checkout(key);
        var paymentId = _db.Context.Orders.Single().PaymentId!;

        var outcome = await _db.Dispatcher.Send(new ProcessPaymentCallbackCommand(
            new Dictionary<string, string?> { ["paymentID"] = paymentId, ["result"] = "success" }));

        Assert.True(outcome.Paid);
        Assert.Equal(checkout.Value!.OrderNumber, outcome.OrderNumber);

        using var fresh = _db.NewContext();
        var order = fresh.Orders.Single();
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.NotNull(order.PaidAt);
        Assert.Equal(7, fresh.Products.Single().Stock);
        Assert.Empty(fresh.Set<CartItem>());
    }

    [Fact]
    public async Task Failed_callback_records_reason_and_keeps_cart()
    {
        var product = _db.AddProduct("Tablet", 650m, 10);
        var key = await GuestCartWith(product.Id, 1);
        await Checkout(key);
        var paymentId = _db.Context.Orders.Single().PaymentId!;

        var outcome = await _db.Dispatcher.Send(new ProcessPaymentCallbackCommand(
            new Dictionary<string, string?> { ["paymentID"] = paymentId, ["result"] = "fail" }));

        Assert.False(outcome.Paid);
        using var fresh = _db.NewContext();
        var order = fresh.Orders.Single();
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.NotNull(order.PaymentFailureReason);
        Assert.Single(fresh.Set<CartItem>());     // cart untouched
        Assert.Equal(10, fresh.Products.Single().Stock);
    }

    [Fact]
    public async Task Replayed_callback_is_idempotent()
    {
        var product = _db.AddProduct("Watch", 329m, 10);
        var key = await GuestCartWith(product.Id, 1);
        await Checkout(key);
        var paymentId = _db.Context.Orders.Single().PaymentId!;
        var query = new Dictionary<string, string?> { ["paymentID"] = paymentId, ["result"] = "success" };

        await _db.Dispatcher.Send(new ProcessPaymentCallbackCommand(query));
        var replay = await _db.Dispatcher.Send(new ProcessPaymentCallbackCommand(query));

        Assert.True(replay.Paid);
        using var fresh = _db.NewContext();
        Assert.Equal(9, fresh.Products.Single().Stock); // decremented once, not twice
    }

    [Fact]
    public async Task Guest_can_read_order_only_with_matching_email()
    {
        var product = _db.AddProduct("TV", 1299m, 10);
        var key = await GuestCartWith(product.Id, 1);
        var checkout = await Checkout(key);
        var orderNumber = checkout.Value!.OrderNumber;

        Assert.NotNull(await _db.Dispatcher.Query(new GetOrderQuery(orderNumber, null, "SHOPPER@example.com")));
        Assert.Null(await _db.Dispatcher.Query(new GetOrderQuery(orderNumber, null, "wrong@example.com")));
        Assert.Null(await _db.Dispatcher.Query(new GetOrderQuery(orderNumber, null, null)));
    }
}
