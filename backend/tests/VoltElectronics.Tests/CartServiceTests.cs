using Microsoft.Extensions.Options;
using VoltElectronics.Application.Cart;
using VoltElectronics.Application.Common;
using VoltElectronics.Infrastructure.Carts;
using VoltElectronics.Infrastructure.Common;

namespace VoltElectronics.Tests;

public class CartServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly CartService _service;

    public CartServiceTests() =>
        _service = new CartService(_db.Context, new CurrencyConverter(Options.Create(new CurrencyOptions())));

    public void Dispose() => _db.Dispose();

    private static CartKey Guest(Guid id) => new(null, id);

    [Fact]
    public async Task Add_creates_guest_cart_and_prices_items()
    {
        var product = _db.AddProduct("Laptop", 1000m, 5);
        var guestId = Guid.NewGuid();

        var cart = await _service.AddItemAsync(Guest(guestId), product.Id, 2);

        Assert.Equal(guestId, cart.Id);
        Assert.Equal(2, cart.Count);
        Assert.Equal(2000m, cart.Subtotal);
        Assert.Equal(24m, cart.Shipping);
        Assert.Equal(2000m + 24m + Math.Round(2000m * 0.0875m, 2), cart.Total);
    }

    [Fact]
    public async Task Add_rejects_quantities_beyond_stock()
    {
        var product = _db.AddProduct("Camera", 500m, 3);
        var key = Guest(Guid.NewGuid());
        await _service.AddItemAsync(key, product.Id, 2);

        await Assert.ThrowsAsync<CartException>(() => _service.AddItemAsync(key, product.Id, 2));
    }

    [Fact]
    public async Task Update_to_zero_removes_the_line()
    {
        var product = _db.AddProduct("Speaker", 100m, 10);
        var key = Guest(Guid.NewGuid());
        await _service.AddItemAsync(key, product.Id, 2);

        var cart = await _service.UpdateItemAsync(key, product.Id, 0);

        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Total);
    }

    [Fact]
    public async Task Merge_reassigns_guest_cart_when_user_has_none()
    {
        var product = _db.AddProduct("Tablet", 600m, 10);
        var guestId = Guid.NewGuid();
        await _service.AddItemAsync(Guest(guestId), product.Id, 1);

        var merged = await _service.MergeAsync(guestId, "user-1");

        Assert.Single(merged.Items);
        var userCart = await _service.GetAsync(new CartKey("user-1", null));
        Assert.Equal(1, userCart.Count);
        // Guest key no longer resolves to that cart.
        var guestCart = await _service.GetAsync(Guest(guestId));
        Assert.Empty(guestCart.Items);
    }

    [Fact]
    public async Task Merge_combines_quantities_and_clamps_to_stock()
    {
        var product = _db.AddProduct("Monitor", 450m, 3);
        var guestId = Guid.NewGuid();
        await _service.AddItemAsync(Guest(guestId), product.Id, 2);
        await _service.AddItemAsync(new CartKey("user-2", null), product.Id, 2);

        var merged = await _service.MergeAsync(guestId, "user-2");

        // 2 + 2 exceeds stock 3 → clamped.
        Assert.Equal(3, merged.Items.Single().Qty);
    }

    [Fact]
    public async Task Merge_without_guest_cart_returns_user_cart_unchanged()
    {
        var product = _db.AddProduct("Phone", 900m, 10);
        await _service.AddItemAsync(new CartKey("user-3", null), product.Id, 1);

        var merged = await _service.MergeAsync(Guid.NewGuid(), "user-3");

        Assert.Equal(1, merged.Count);
    }
}
