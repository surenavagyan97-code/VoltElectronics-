using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VoltElectronics.Application.Cart;
using VoltElectronics.Application.Orders;
using VoltElectronics.Domain.Enums;
using VoltElectronics.Infrastructure.Carts;
using VoltElectronics.Infrastructure.Orders;
using VoltElectronics.Infrastructure.Payments;

namespace VoltElectronics.Tests;

public class OrderServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly OrderService _service;
    private readonly CartService _cartService;

    private static readonly CheckoutRequest ValidRequest = new(
        "shopper@example.com", "Jordan Lee", null, "500 Market St", "Yerevan", "Yerevan", "0010", null);

    public OrderServiceTests()
    {
        var options = Options.Create(new PaymentsOptions());
        _service = new OrderService(_db.Context, new FakePaymentProvider(), options, NullLogger<OrderService>.Instance);
        _cartService = new CartService(_db.Context);
    }

    public void Dispose() => _db.Dispose();

    private async Task<CartKey> GuestCartWith(int productId, int qty)
    {
        var key = new CartKey(null, Guid.NewGuid());
        await _cartService.AddItemAsync(key, productId, qty);
        return key;
    }

    [Fact]
    public async Task Checkout_fails_on_empty_cart()
    {
        var result = await _service.CheckoutAsync(new CartKey(null, Guid.NewGuid()), null, ValidRequest);

        Assert.False(result.Success);
        Assert.Contains("empty", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checkout_fails_when_stock_dropped_below_cart_quantity()
    {
        var product = _db.AddProduct("Console", 500m, 5);
        var key = await GuestCartWith(product.Id, 4);

        product.Stock = 2; // someone else bought the rest
        await _db.Context.SaveChangesAsync();

        var result = await _service.CheckoutAsync(key, null, ValidRequest);

        Assert.False(result.Success);
        Assert.Contains("left in stock", result.Error);
    }

    [Fact]
    public async Task Checkout_reprices_server_side_and_creates_pending_order_with_payment()
    {
        var product = _db.AddProduct("Laptop", 1000m, 10);
        var key = await GuestCartWith(product.Id, 2);

        var result = await _service.CheckoutAsync(key, null, ValidRequest);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Contains("/api/payments/fake/pay", result.Data!.PaymentUrl);

        var order = _db.NewContext().Orders.Single();
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(2000m, order.Subtotal);
        Assert.Equal(2000m + 24m + Math.Round(2000m * 0.0875m, 2), order.Total);
        Assert.Equal("Fake", order.PaymentProvider);
        Assert.False(string.IsNullOrEmpty(order.PaymentId));
    }

    [Fact]
    public async Task Successful_callback_marks_paid_decrements_stock_and_clears_cart()
    {
        var product = _db.AddProduct("Camera", 1100m, 10);
        var key = await GuestCartWith(product.Id, 3);
        var checkout = await _service.CheckoutAsync(key, null, ValidRequest);
        var paymentId = _db.Context.Orders.Single().PaymentId!;

        var outcome = await _service.HandleCallbackAsync(new Dictionary<string, string?>
        {
            ["paymentID"] = paymentId,
            ["result"] = "success",
        });

        Assert.True(outcome.Paid);
        Assert.Equal(checkout.Data!.OrderNumber, outcome.OrderNumber);

        using var fresh = _db.NewContext();
        var order = fresh.Orders.Single();
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.NotNull(order.PaidAt);
        Assert.Equal(7, fresh.Products.Single().Stock);
        Assert.Empty(fresh.CartItems);
    }

    [Fact]
    public async Task Failed_callback_records_reason_and_keeps_cart()
    {
        var product = _db.AddProduct("Tablet", 650m, 10);
        await GuestCartWith(product.Id, 1);
        await _service.CheckoutAsync(new CartKey(null, _db.Context.Carts.Single().Id), null, ValidRequest);
        var paymentId = _db.Context.Orders.Single().PaymentId!;

        var outcome = await _service.HandleCallbackAsync(new Dictionary<string, string?>
        {
            ["paymentID"] = paymentId,
            ["result"] = "fail",
        });

        Assert.False(outcome.Paid);
        using var fresh = _db.NewContext();
        var order = fresh.Orders.Single();
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.NotNull(order.PaymentFailureReason);
        Assert.Single(fresh.CartItems);           // cart untouched
        Assert.Equal(10, fresh.Products.Single().Stock);
    }

    [Fact]
    public async Task Replayed_callback_is_idempotent()
    {
        var product = _db.AddProduct("Watch", 329m, 10);
        var key = await GuestCartWith(product.Id, 1);
        await _service.CheckoutAsync(key, null, ValidRequest);
        var paymentId = _db.Context.Orders.Single().PaymentId!;
        var query = new Dictionary<string, string?> { ["paymentID"] = paymentId, ["result"] = "success" };

        await _service.HandleCallbackAsync(query);
        var replay = await _service.HandleCallbackAsync(query);

        Assert.True(replay.Paid);
        Assert.Equal(9, _db.NewContext().Products.Single().Stock); // decremented once, not twice
    }

    [Fact]
    public async Task Guest_can_read_order_only_with_matching_email()
    {
        var product = _db.AddProduct("TV", 1299m, 10);
        var key = await GuestCartWith(product.Id, 1);
        var checkout = await _service.CheckoutAsync(key, null, ValidRequest);
        var orderNumber = checkout.Data!.OrderNumber;

        Assert.NotNull(await _service.GetOrderAsync(orderNumber, null, "SHOPPER@example.com"));
        Assert.Null(await _service.GetOrderAsync(orderNumber, null, "wrong@example.com"));
        Assert.Null(await _service.GetOrderAsync(orderNumber, null, null));
    }
}
