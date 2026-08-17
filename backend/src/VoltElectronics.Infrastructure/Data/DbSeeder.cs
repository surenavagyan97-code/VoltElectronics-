using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoltElectronics.Application.Identity;
using VoltElectronics.Domain.Catalog;
using VoltElectronics.Domain.Content;
using VoltElectronics.Domain.Ordering;
using VoltElectronics.Infrastructure.Identity;

namespace VoltElectronics.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@volt.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Volt Admin"
            };
            var password = config["Seed:AdminPassword"] ?? "Admin123$";
            var result = await userManager.CreateAsync(admin, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
            else
                logger.LogWarning("Admin seed failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // Editable pages get their initial default-language body once; after that the admin panel
        // owns the text (including translations), so re-seeding must never overwrite it.
        foreach (var (key, body) in DefaultContent.Pages)
        {
            if (!await db.ContentPages.AnyAsync(p => p.Key == key))
                db.ContentPages.Add(ContentPage.Create(key, ContentPage.DefaultLang, body));
        }
        await db.SaveChangesAsync();

        if (await db.Categories.AnyAsync())
            return;

        var cats = new[] { "Laptops", "Phones", "Audio", "TVs", "Wearables", "Cameras", "Tablets", "Monitors", "Gaming" }
            .Select(Category.Create)
            .ToDictionary(c => c.Name);
        db.Categories.AddRange(cats.Values);
        // Saved before products are built: Product references its category by id, not navigation.
        await db.SaveChangesAsync();

        Product P(string name, string cat, decimal price, int stock, double rating, int reviews, string sku,
                  string desc, decimal? oldPrice = null, string? badge = null, params (string n, string v)[] specs)
        {
            var product = Product.Create(
                name, Slug.From(name), sku, cats[cat].Id, desc,
                price, oldPrice, stock, ProductStatus.Active, badge);
            product.SetRating(rating, reviews);
            product.ReplaceSpecs(specs.Select(s => (s.n, s.v)));
            return product;
        }

        var products = new List<Product>
        {
            P("Aurora Pro 15 Laptop", "Laptops", 1499m, 24, 4.8, 312, "VLT-LP-2201",
              "A 15-inch aluminum-unibody workstation built for sustained performance: 14-core processor, 32GB unified memory and a 1TB NVMe drive, tuned for all-day battery life under real workloads.",
              1699m, "New", ("Processor", "14-core, 4.2GHz boost"), ("Memory", "32GB unified"), ("Storage", "1TB NVMe SSD")),
            P("Halcyon X1 Smartphone", "Phones", 899m, 56, 4.6, 528, "VLT-PH-1108",
              "A 6.5-inch flagship with a pro-grade triple camera, two-day adaptive battery and five years of guaranteed updates.",
              null, "Best seller", ("Display", "6.5\" OLED, 120Hz"), ("Camera", "50MP triple system"), ("Battery", "5,100 mAh")),
            P("Nimbus Wireless Headphones", "Audio", 249m, 120, 4.7, 861, "VLT-AU-0341",
              "Over-ear active noise cancelling headphones with 40-hour battery life, multipoint Bluetooth and a travel-flat hinge.",
              null, null, ("Driver", "40mm dynamic"), ("Battery", "40h with ANC"), ("Connectivity", "Bluetooth 5.4, multipoint")),
            P("Vantage 55\" OLED TV", "TVs", 1299m, 12, 4.5, 204, "VLT-TV-5502",
              "A 55-inch 4K OLED panel with 120Hz refresh, Dolby Vision and a near-invisible stand — built for movie nights and next-gen consoles alike.",
              1499m, "Sale", ("Panel", "55\" 4K OLED, 120Hz"), ("HDR", "Dolby Vision / HDR10+"), ("Inputs", "4× HDMI 2.1")),
            P("Pulse Fitness Smartwatch", "Wearables", 329m, 80, 4.4, 442, "VLT-WR-0790",
              "Multi-band GPS, dual-sensor heart-rate tracking and a 10-day battery in a 42g titanium case.",
              null, null, ("Battery", "10 days typical"), ("Sensors", "HR, SpO2, GPS multi-band"), ("Water rating", "5 ATM")),
            P("Cascade Mirrorless Camera", "Cameras", 1099m, 18, 4.9, 176, "VLT-CM-0233",
              "A 26MP APS-C mirrorless body with in-body stabilization, 6K open-gate video and dual UHS-II card slots.",
              null, null, ("Sensor", "26MP APS-C"), ("Video", "6K30 open gate"), ("Stabilization", "5-axis IBIS")),
            P("Drift Bluetooth Speaker", "Audio", 129m, 200, 4.3, 1093, "VLT-AU-0512",
              "A pocketable IP67 speaker with surprisingly deep bass, 20-hour playtime and stereo pairing.",
              null, null, ("Battery", "20h playtime"), ("Rating", "IP67 dust/waterproof"), ("Pairing", "Stereo TWS")),
            P("Solace 11 Tablet", "Tablets", 649m, 40, 4.6, 389, "VLT-TB-1104",
              "An 11-inch 120Hz tablet with laptop-class silicon, quad speakers and all-day battery — pen and keyboard ready.",
              null, null, ("Display", "11\" LCD, 120Hz"), ("Storage", "256GB"), ("Battery", "10h mixed use")),
            P("Meridian 27\" 4K Monitor", "Monitors", 449m, 33, 4.7, 267, "VLT-MN-2704",
              "A factory-calibrated 27-inch 4K IPS display with 98% DCI-P3, USB-C 90W passthrough and a height-adjustable stand.",
              null, null, ("Panel", "27\" 4K IPS"), ("Color", "98% DCI-P3, factory calibrated"), ("Connectivity", "USB-C 90W PD")),
            P("Apex Gaming Console", "Gaming", 499m, 15, 4.8, 731, "VLT-GM-0917",
              "A living-room console with 2TB of fast storage, 4K120 output and near-silent cooling.",
              null, "New", ("Storage", "2TB NVMe"), ("Output", "4K @ 120Hz, VRR"), ("Audio", "3D spatial audio")),
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        // Demo orders over the past 30 days so admin analytics/orders pages have data on first run.
        var rng = new Random(20260803);
        string[] customers = ["Priya Nair", "Marcus Chen", "Sofia Reyes", "Tom Becker", "Amara Obi", "Liam Walsh"];
        string[] cities = ["San Francisco", "Austin", "Seattle", "Denver", "Chicago", "Boston"];
        string[] states = ["CA", "TX", "WA", "CO", "IL", "MA"];
        var now = DateTime.UtcNow;

        for (var i = 0; i < 32; i++)
        {
            var daysAgo = rng.Next(0, 30);
            var created = now.AddDays(-daysAgo).AddHours(-rng.Next(0, 24));
            var ci = rng.Next(customers.Length);
            var itemCount = rng.Next(1, 4);
            var picked = Enumerable.Range(0, itemCount)
                .Select(_ => products[rng.Next(products.Count)])
                .DistinctBy(p => p.Id)
                .ToList();

            var lines = picked
                .Select(p => new OrderLine(p.Id, p.Name, p.Price, rng.Next(1, 3)))
                .ToList();

            var (subtotal, discount, shipping, tax, total) = PricingPolicy.Totals(lines.Sum(l => l.UnitPrice * l.Qty));
            var status = daysAgo switch
            {
                > 14 => OrderStatus.Delivered,
                > 7 => rng.Next(4) == 0 ? OrderStatus.Cancelled : OrderStatus.Delivered,
                > 3 => OrderStatus.Shipped,
                _ => rng.Next(3) == 0 ? OrderStatus.Shipped : OrderStatus.Processing
            };

            var order = Order.Place(
                $"ORD-{58150 + i}",
                userId: null,
                email: customers[ci].Split(' ')[0].ToLowerInvariant() + "@example.com",
                ShippingAddress.Create(
                    customers[ci], null, $"{100 + rng.Next(900)} Market St",
                    cities[ci], states[ci], $"{94000 + rng.Next(5000)}", "+1 555 0100"),
                new OrderTotals(subtotal, discount, shipping, tax, total, "USD", 1m),
                cartId: null,
                paymentProvider: "Fake",
                lines);
            order.ChangeStatus(status);
            db.Orders.Add(order);

            // Demo data is deliberately backdated; these setters are private on purpose, and going
            // through MarkPaid would also draw down stock — so write the two audit stamps directly.
            db.Entry(order).Property(o => o.CreatedAt).CurrentValue = created;
            if (status != OrderStatus.Cancelled)
                db.Entry(order).Property(o => o.PaidAt).CurrentValue = created.AddMinutes(2);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Database seeded: {Products} products, 32 demo orders", products.Count);
    }
}
