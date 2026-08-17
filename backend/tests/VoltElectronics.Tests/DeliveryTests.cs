using VoltElectronics.Application.Delivery;
using VoltElectronics.Application.Delivery.Queries;
using VoltElectronics.Domain.Ordering;

namespace VoltElectronics.Tests;

public class DeliveryTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private Order AddOrder(string orderNumber, string? courierId = null, OrderStatus? status = null)
    {
        var product = _db.AddProduct($"Gadget-{orderNumber}", 100m, 10);
        var order = Order.Place(
            orderNumber, null, "shopper@example.com",
            ShippingAddress.Create("Jordan Lee", null, "500 Market St", "Yerevan", "Yerevan", "0010", "+374 91 000000"),
            new OrderTotals(100m, 0m, 10m, 5m, 115m, "USD", 1m),
            null, "Fake",
            [new OrderLine(product.Id, product.Name, 100m, 1)]);
        if (status is { } s) order.ChangeStatus(s);
        order.AssignCourier(courierId);
        _db.Context.Orders.Add(order);
        _db.Context.SaveChanges();
        return order;
    }

    [Fact]
    public async Task Courier_marks_own_order_delivered()
    {
        AddOrder("VE-1001", courierId: "courier-1", status: OrderStatus.Shipped);

        var result = await _db.Dispatcher.Send(
            new UpdateDeliveryOrderStatusCommand("VE-1001", "courier-1", "Delivered"));

        Assert.True(result.IsSuccess);
        using var fresh = _db.NewContext();
        Assert.Equal(OrderStatus.Delivered, fresh.Orders.Single().Status);
    }

    [Fact]
    public async Task Courier_cannot_set_statuses_reserved_for_admins()
    {
        AddOrder("VE-1002", courierId: "courier-1", status: OrderStatus.Processing);

        var result = await _db.Dispatcher.Send(
            new UpdateDeliveryOrderStatusCommand("VE-1002", "courier-1", "Cancelled"));

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Courier_cannot_touch_an_order_assigned_to_someone_else()
    {
        AddOrder("VE-1003", courierId: "courier-1", status: OrderStatus.Processing);

        var result = await _db.Dispatcher.Send(
            new UpdateDeliveryOrderStatusCommand("VE-1003", "courier-2", "Shipped"));

        Assert.False(result.IsSuccess);
        using var fresh = _db.NewContext();
        Assert.Equal(OrderStatus.Processing, fresh.Orders.Single().Status);
    }

    [Fact]
    public async Task Delivery_list_returns_only_the_couriers_orders_with_address_and_total()
    {
        AddOrder("VE-2001", courierId: "courier-1", status: OrderStatus.Processing);
        AddOrder("VE-2002", courierId: "courier-2", status: OrderStatus.Processing);
        AddOrder("VE-2003", courierId: null);

        var orders = await _db.Dispatcher.Query(new GetDeliveryOrdersQuery("courier-1"));

        var order = Assert.Single(orders);
        Assert.Equal("VE-2001", order.OrderNumber);
        Assert.Equal("500 Market St", order.Street);
        Assert.Equal("Jordan Lee", order.FullName);
        Assert.Equal(115m, order.Total);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task Delivery_list_filters_by_status()
    {
        AddOrder("VE-3001", courierId: "courier-1", status: OrderStatus.Processing);
        AddOrder("VE-3002", courierId: "courier-1", status: OrderStatus.Delivered);

        var delivered = await _db.Dispatcher.Query(new GetDeliveryOrdersQuery("courier-1", "Delivered"));

        Assert.Equal("VE-3002", Assert.Single(delivered).OrderNumber);
    }
}
