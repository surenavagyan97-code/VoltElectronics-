using VoltElectronics.Application.Admin;
using VoltElectronics.Application.Admin.Categories;
using VoltElectronics.Application.Admin.Products;
using VoltElectronics.Application.Carts;
using VoltElectronics.Application.Carts.Commands;
using VoltElectronics.Application.Common.Results;
using VoltElectronics.Application.Ordering;
using VoltElectronics.Application.Ordering.Commands;
using VoltElectronics.Domain.Carts;

namespace VoltElectronics.Tests;

/// <summary>
/// Sends invalid commands through the real dispatcher and asserts they come back as failed
/// Results from ValidationBehavior — before any handler or transaction runs.
/// </summary>
public class ValidationBehaviorTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static SaveProductRequest Product(string name = "Widget", decimal price = 10m, string sku = "SKU-1") =>
        new(name, sku, CategoryId: 1, "desc", price, null, Stock: 1, "Active", null, null);

    [Fact]
    public async Task Create_product_with_blank_name_and_negative_price_fails_with_both_messages()
    {
        var result = await _db.Dispatcher.Send(new CreateProductCommand(Product(name: "", price: -5m)));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Invalid, result.Error!.Kind);
        Assert.Contains("Name", result.Error.Message);
        Assert.Contains("Price", result.Error.Message);
    }

    [Fact]
    public async Task Create_product_with_unknown_status_fails()
    {
        var result = await _db.Dispatcher.Send(new CreateProductCommand(
            Product() with { Status = "Discontinued" }));

        Assert.False(result.IsSuccess);
        Assert.Contains("Status must be Active, Draft or Archived.", result.Error!.Message);
    }

    [Fact]
    public async Task Valid_commands_pass_through_to_their_handler()
    {
        // The handler, not the validator, rejects this one — proof the pipeline lets a
        // well-formed command reach the database checks.
        var result = await _db.Dispatcher.Send(new CreateProductCommand(
            Product() with { CategoryId = 999 }));

        Assert.False(result.IsSuccess);
        Assert.Equal("Category not found.", result.Error!.Message);
    }

    [Fact]
    public async Task Category_name_over_column_limit_fails()
    {
        var result = await _db.Dispatcher.Send(new CreateCategoryCommand(
            new SaveCategoryRequest(new string('x', 101))));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Invalid, result.Error!.Kind);
    }

    [Fact]
    public async Task Add_cart_item_with_non_positive_qty_fails_without_touching_the_cart()
    {
        var product = _db.AddProduct("Cable", 15m, 10);
        var key = new CartKey(null, Guid.NewGuid());

        var result = await _db.Dispatcher.Send(new AddCartItemCommand(key, product.Id, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Invalid, result.Error!.Kind);
        using var fresh = _db.NewContext();
        Assert.Empty(fresh.Set<CartItem>());
    }

    [Fact]
    public async Task Checkout_with_malformed_email_fails_before_the_handler()
    {
        var result = await _db.Dispatcher.Send(new CheckoutCommand(
            new CartKey(null, Guid.NewGuid()), null,
            new CheckoutRequest("not-an-email", "Jordan Lee", null, "500 Market St", "Yerevan", "Yerevan", "0010", null)));

        Assert.False(result.IsSuccess);
        // The empty-cart check lives in the handler; seeing the email complaint instead proves
        // validation ran first.
        Assert.Contains("Email", result.Error!.Message);
    }
}
