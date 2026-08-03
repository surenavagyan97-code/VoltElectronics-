using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VoltElectronics.Application.Auth;
using VoltElectronics.Infrastructure.Auth;
using VoltElectronics.Infrastructure.Data;
using VoltElectronics.Infrastructure.Identity;

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

        return services;
    }
}
