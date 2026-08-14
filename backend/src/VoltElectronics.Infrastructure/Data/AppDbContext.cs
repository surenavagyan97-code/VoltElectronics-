using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Carts;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Content;
using VoltElectronics.Domain.Identity;
using VoltElectronics.Domain.Ordering;
using VoltElectronics.Infrastructure.Identity;

namespace VoltElectronics.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(e =>
        {
            e.Ignore(c => c.DomainEvents);
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Slug).HasMaxLength(120);
            e.HasIndex(c => c.Slug).IsUnique();
        });

        builder.Entity<Product>(e =>
        {
            e.Ignore(p => p.DomainEvents);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Slug).HasMaxLength(220);
            e.Property(p => p.Sku).HasMaxLength(50);
            e.Property(p => p.Badge).HasMaxLength(40);
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.CompareAtPrice).HasPrecision(18, 2);
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId);
            e.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Specs).WithOne().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContentPage>(e =>
        {
            e.Ignore(p => p.DomainEvents);
            e.Property(p => p.Key).HasMaxLength(50);
            e.HasIndex(p => p.Key).IsUnique();
        });

        // Child entities are reachable only through their aggregate root, so they have no DbSet —
        // pin the table names the old per-entity DbSets produced.
        builder.Entity<ProductImage>(e =>
        {
            e.ToTable("ProductImages");
            e.Property(i => i.Url).HasMaxLength(500);
        });

        builder.Entity<ProductSpec>(e =>
        {
            e.ToTable("ProductSpecs");
            e.Property(s => s.Name).HasMaxLength(100);
            e.Property(s => s.Value).HasMaxLength(300);
        });

        builder.Entity<Cart>(e =>
        {
            e.Ignore(c => c.DomainEvents);
            // Guest cart ids are minted client-side and travel in the X-Cart-Id header.
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.Currency).HasMaxLength(3);
            e.HasIndex(c => c.UserId);
            e.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(e =>
        {
            e.ToTable("CartItems");
            e.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
            e.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId);
        });

        builder.Entity<Order>(e =>
        {
            e.Ignore(o => o.DomainEvents);
            e.Property(o => o.OrderNumber).HasMaxLength(20);
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.UserId);
            e.HasIndex(o => o.PaymentId);
            e.Property(o => o.GuestEmail).HasMaxLength(256);
            e.Property(o => o.PaymentId).HasMaxLength(100);
            e.Property(o => o.PaymentProvider).HasMaxLength(30);
            e.Property(o => o.PaymentFailureReason).HasMaxLength(500);

            // Both value objects share the Orders table under the column names the original flat
            // entity used, so no migration is needed for the aggregate redesign.
            e.OwnsOne(o => o.ShipTo, ship =>
            {
                ship.Property(s => s.FullName).HasColumnName("ShipFullName").HasMaxLength(150);
                ship.Property(s => s.Company).HasColumnName("ShipCompany").HasMaxLength(150);
                ship.Property(s => s.Street).HasColumnName("ShipStreet").HasMaxLength(250);
                ship.Property(s => s.City).HasColumnName("ShipCity").HasMaxLength(100);
                ship.Property(s => s.State).HasColumnName("ShipState").HasMaxLength(50);
                ship.Property(s => s.Zip).HasColumnName("ShipZip").HasMaxLength(20);
                ship.Property(s => s.Phone).HasColumnName("ShipPhone").HasMaxLength(30);
            });
            e.Navigation(o => o.ShipTo).IsRequired();

            e.OwnsOne(o => o.Totals, totals =>
            {
                totals.Property(t => t.Subtotal).HasColumnName("Subtotal").HasPrecision(18, 2);
                totals.Property(t => t.Shipping).HasColumnName("ShippingCost").HasPrecision(18, 2);
                totals.Property(t => t.Tax).HasColumnName("Tax").HasPrecision(18, 2);
                totals.Property(t => t.Total).HasColumnName("Total").HasPrecision(18, 2);
                totals.Property(t => t.Currency).HasColumnName("Currency").HasMaxLength(3);
                totals.Property(t => t.ExchangeRate).HasColumnName("ExchangeRate").HasPrecision(18, 6);
            });
            e.Navigation(o => o.Totals).IsRequired();

            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.ToTable("OrderItems");
            e.Property(i => i.ProductName).HasMaxLength(200);
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);
            // Keep order history when a product is removed from the catalog.
            e.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.Ignore(t => t.DomainEvents);
            e.Property(t => t.TokenHash).HasMaxLength(128);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId);
        });
    }
}
