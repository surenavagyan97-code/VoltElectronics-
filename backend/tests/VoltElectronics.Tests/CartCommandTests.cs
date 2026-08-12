using VoltElectronics.Application.Carts;
using VoltElectronics.Application.Carts.Commands;
using VoltElectronics.Application.Carts.Queries;
using VoltElectronics.Domain.Common;

namespace VoltElectronics.Tests;

public class CartCommandTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static CartKey Guest(Guid id) => new(null, id);

    [Fact]
    public async Task Add_creates_guest_cart_and_prices_items()
    {
        var product = _db.AddProduct("Laptop", 1000m, 5);
        var guestId = Guid.NewGuid();

        var result = await _db.Dispatcher.Send(new AddCartItemCommand(Guest(guestId), product.Id, 2));

        Assert.True(result.IsSuccess);
        var cart = result.Value!;
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
        await _db.Dispatcher.Send(new AddCartItemCommand(key, product.Id, 2));

        // Stock rules are the Cart aggregate's own; a violation surfaces as a DomainException (400).
        await Assert.ThrowsAsync<DomainException>(() =>
            _db.Dispatcher.Send(new AddCartItemCommand(key, product.Id, 2)));
    }

    [Fact]
    public async Task Update_to_zero_removes_the_line()
    {
        var product = _db.AddProduct("Speaker", 100m, 10);
        var key = Guest(Guid.NewGuid());
        await _db.Dispatcher.Send(new AddCartItemCommand(key, product.Id, 2));

        var result = await _db.Dispatcher.Send(new UpdateCartItemQtyCommand(key, product.Id, 0));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0m, result.Value!.Total);
    }

    [Fact]
    public async Task Merge_reassigns_guest_cart_when_user_has_none()
    {
        var product = _db.AddProduct("Tablet", 600m, 10);
        var guestId = Guid.NewGuid();
        await _db.Dispatcher.Send(new AddCartItemCommand(Guest(guestId), product.Id, 1));

        var merged = await _db.Dispatcher.Send(new MergeGuestCartCommand(guestId, "user-1"));

        Assert.Single(merged.Value!.Items);
        var userCart = await _db.Dispatcher.Query(new GetCartQuery(new CartKey("user-1", null)));
        Assert.Equal(1, userCart.Count);
        // Guest key no longer resolves to that cart.
        var guestCart = await _db.Dispatcher.Query(new GetCartQuery(Guest(guestId)));
        Assert.Empty(guestCart.Items);
    }

    [Fact]
    public async Task Merge_combines_quantities_and_clamps_to_stock()
    {
        var product = _db.AddProduct("Monitor", 450m, 3);
        var guestId = Guid.NewGuid();
        await _db.Dispatcher.Send(new AddCartItemCommand(Guest(guestId), product.Id, 2));
        await _db.Dispatcher.Send(new AddCartItemCommand(new CartKey("user-2", null), product.Id, 2));

        var merged = await _db.Dispatcher.Send(new MergeGuestCartCommand(guestId, "user-2"));

        // 2 + 2 exceeds stock 3 → clamped.
        Assert.Equal(3, merged.Value!.Items.Single().Qty);
    }

    [Fact]
    public async Task Merge_without_guest_cart_returns_user_cart_unchanged()
    {
        var product = _db.AddProduct("Phone", 900m, 10);
        await _db.Dispatcher.Send(new AddCartItemCommand(new CartKey("user-3", null), product.Id, 1));

        var merged = await _db.Dispatcher.Send(new MergeGuestCartCommand(Guid.NewGuid(), "user-3"));

        Assert.Equal(1, merged.Value!.Count);
    }
}
