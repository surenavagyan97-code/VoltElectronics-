using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoltElectronics.Domain.Entities;
using VoltElectronics.Infrastructure.Identity;

namespace VoltElectronics.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSpec> ProductSpecs => Set<ProductSpec>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Slug).HasMaxLength(120);
            e.HasIndex(c => c.Slug).IsUnique();
        });

        builder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Slug).HasMaxLength(220);
            e.Property(p => p.Sku).HasMaxLength(50);
            e.Property(p => p.Badge).HasMaxLength(40);
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.CompareAtPrice).HasPrecision(18, 2);
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
            e.HasMany(p => p.Images).WithOne(i => i.Product).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Specs).WithOne(s => s.Product).HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductImage>(e => e.Property(i => i.Url).HasMaxLength(500));

        builder.Entity<ProductSpec>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(100);
            e.Property(s => s.Value).HasMaxLength(300);
        });

        builder.Entity<Cart>(e =>
        {
            e.Property(c => c.Id).ValueGeneratedNever();
            e.HasIndex(c => c.UserId);
            e.HasMany(c => c.Items).WithOne(i => i.Cart).HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(e =>
        {
            e.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
        });

        builder.Entity<Order>(e =>
        {
            e.Property(o => o.OrderNumber).HasMaxLength(20);
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasIndex(o => o.UserId);
            e.HasIndex(o => o.PaymentId);
            e.Property(o => o.GuestEmail).HasMaxLength(256);
            e.Property(o => o.ShipFullName).HasMaxLength(150);
            e.Property(o => o.ShipCompany).HasMaxLength(150);
            e.Property(o => o.ShipStreet).HasMaxLength(250);
            e.Property(o => o.ShipCity).HasMaxLength(100);
            e.Property(o => o.ShipState).HasMaxLength(50);
            e.Property(o => o.ShipZip).HasMaxLength(20);
            e.Property(o => o.ShipPhone).HasMaxLength(30);
            e.Property(o => o.PaymentId).HasMaxLength(100);
            e.Property(o => o.PaymentProvider).HasMaxLength(30);
            e.Property(o => o.PaymentFailureReason).HasMaxLength(500);
            e.Property(o => o.Subtotal).HasPrecision(18, 2);
            e.Property(o => o.ShippingCost).HasPrecision(18, 2);
            e.Property(o => o.Tax).HasPrecision(18, 2);
            e.Property(o => o.Total).HasPrecision(18, 2);
            e.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.ProductName).HasMaxLength(200);
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);
            // Keep order history when a product is removed from the catalog.
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.Property(t => t.TokenHash).HasMaxLength(128);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId);
        });
    }
}
