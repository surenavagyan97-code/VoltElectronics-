using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoltElectronics.Application.Common;
using VoltElectronics.Application.Common.Abstractions;
using VoltElectronics.Application.Common.Messaging;
using VoltElectronics.Domain.Common;
using VoltElectronics.Infrastructure.Auth;
using VoltElectronics.Infrastructure.Common;
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
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

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
        services.AddScoped<TokenIssuer>();

        services.Configure<CurrencyOptions>(config.GetSection(CurrencyOptions.SectionName));
        services.AddSingleton<ICurrencyConverter, CurrencyConverter>();
        services.AddSingleton<IOrderNumberGenerator, OrderNumberGenerator>();

        services.AddPersistenceAdapters();

        services.Configure<PaymentsOptions>(config.GetSection(PaymentsOptions.SectionName));
        if (string.Equals(config["Payments:Provider"], "Ameria", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IPaymentGateway, AmeriaVposGateway>();
        else
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

        return services;
    }

    /// <summary>
    /// Everything that hangs off the DbContext, minus the DbContext itself: the unit of work, the
    /// repositories and readers (bound by convention — adding one is enough to register it), and
    /// this assembly's query/command handlers. Split out so tests can run the real persistence
    /// graph over an in-memory database.
    /// </summary>
    public static IServiceCollection AddPersistenceAdapters(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository") || t.Name.EndsWith("Reader")), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // The read side (query handlers projecting straight off the DbContext) and the identity
        // command handlers live in this assembly; the same scan Application runs on itself.
        services.AddMessageHandlersFrom(typeof(DependencyInjection).Assembly);

        // Command middleware around every handler registered above (Application's included —
        // AddApplication always runs before this). Decorate order matters: each call wraps the
        // previous, so validation ends up outermost and an invalid command is rejected before a
        // transaction is ever opened. Keep these the last word on ICommandHandler<,> registrations
        // or late additions would escape both behaviors.
        services.Decorate(typeof(ICommandHandler<,>), typeof(TransactionBehavior<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
