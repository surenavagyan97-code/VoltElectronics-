using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Entities;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Tests;

/// <summary>SQLite in-memory AppDbContext; the connection is held open for the fixture's lifetime.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        Context.Database.EnsureCreated();
    }

    /// <summary>Fresh context over the same database — verifies data actually round-trips.</summary>
    public AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public Product AddProduct(string name, decimal price, int stock)
    {
        var category = Context.Categories.FirstOrDefault() ?? new Category { Name = "Test", Slug = "test" };
        var product = new Product
        {
            Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
            Sku = $"SKU-{name.GetHashCode():X}",
            Category = category,
            Price = price,
            Stock = stock,
        };
        Context.Products.Add(product);
        Context.SaveChanges();
        return product;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
