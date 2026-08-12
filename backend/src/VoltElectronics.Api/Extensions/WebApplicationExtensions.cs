using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VoltElectronics.Infrastructure.Data;

namespace VoltElectronics.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Serves uploaded product images. wwwroot doesn't exist until the first image upload creates
    /// it; ASP.NET Core resolves WebRootPath once at host build time, so the default
    /// UseStaticFiles() would bind to a NullFileProvider and 404 forever if wwwroot was missing at
    /// startup. Create the folder up front and point an explicit PhysicalFileProvider at it
    /// instead of relying on the WebRootPath convention.
    /// </summary>
    public static WebApplication UseUploadedFiles(this WebApplication app)
    {
        var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(webRoot);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(webRoot) });
        return app;
    }

    /// <summary>Apply migrations + seed on startup; retry while MSSQL (docker) is still coming up.</summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                await DbSeeder.SeedAsync(scope.ServiceProvider);
                break;
            }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/10): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
