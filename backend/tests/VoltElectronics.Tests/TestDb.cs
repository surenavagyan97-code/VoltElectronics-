using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VoltElectronics.Application;
using VoltElectronics.Application.Common;
using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Infrastructure;
using VoltElectronics.Infrastructure.Common;
using VoltElectronics.Infrastructure.Data;
using VoltElectronics.Infrastructure.Payments;

namespace VoltElectronics.Tests;

/// <summary>
/// SQLite in-memory database under the real composition: the dispatcher, every Application and
/// Infrastructure handler, repositories and the unit of work — exactly what the API runs, with
/// only the database and gateway swapped for test doubles.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public AppDbContext Context { get; }
    public IDispatcher Dispatcher { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddApplication();
        services.AddPersistenceAdapters();
        services.AddSingleton<ICurrencyConverter>(new CurrencyConverter(Options.Create(new CurrencyOptions())));
        services.AddSingleton<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddSingleton<IPaymentGateway>(new FakePaymentGateway(Options.Create(new PaymentsOptions())));
        _provider = services.BuildServiceProvider();

        // One scope for the whole fixture — the same "one unit of work per request" shape the API has.
        _scope = _provider.CreateScope();
        Context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Context.Database.EnsureCreated();
        Dispatcher = _scope.ServiceProvider.GetRequiredService<IDispatcher>();
    }

    /// <summary>Fresh context over the same database — verifies data actually round-trips.</summary>
    public AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public Product AddProduct(string name, decimal price, int stock)
    {
        var category = Context.Categories.FirstOrDefault();
        if (category is null)
        {
            category = Category.Create("Test");
            Context.Categories.Add(category);
            Context.SaveChanges();
        }

        var product = Product.Create(
            name, Slug.From(name), $"SKU-{name.GetHashCode():X}", category.Id, null,
            price, null, stock, ProductStatus.Active, null);
        Context.Products.Add(product);
        Context.SaveChanges();
        return product;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }
}
