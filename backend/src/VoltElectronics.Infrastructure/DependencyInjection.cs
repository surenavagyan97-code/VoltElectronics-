using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoltElectronics.Application.Auth;
using VoltElectronics.Application.Cart;
using VoltElectronics.Application.Catalog;
using VoltElectronics.Application.Common;
using VoltElectronics.Application.Orders;
using VoltElectronics.Application.Payments;
using VoltElectronics.Application.Admin;
using VoltElectronics.Infrastructure.Admin;
using VoltElectronics.Infrastructure.Auth;
using VoltElectronics.Infrastructure.Carts;
using VoltElectronics.Infrastructure.Catalog;
using VoltElectronics.Infrastructure.Common;
using VoltElectronics.Infrastructure.Orders;
using VoltElectronics.Infrastructure.Data;
using VoltElectronics.Infrastructure.Identity;
using VoltElectronics.Infrastructure.Payments;

namespace VoltElectronics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default")));

        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<CurrencyOptions>(config.GetSection(CurrencyOptions.SectionName));
        services.AddSingleton<ICurrencyConverter, CurrencyConverter>();

        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminService, AdminService>();

        services.Configure<PaymentsOptions>(config.GetSection(PaymentsOptions.SectionName));
        if (string.Equals(config["Payments:Provider"], "Ameria", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IPaymentProvider, AmeriaVposProvider>();
        else
            services.AddSingleton<IPaymentProvider, FakePaymentProvider>();

        return services;
    }
}
