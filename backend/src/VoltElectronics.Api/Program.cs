using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using VoltElectronics.Infrastructure;
using VoltElectronics.Infrastructure.Auth;
using VoltElectronics.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Volt Electronics API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// wwwroot doesn't exist until the first image upload creates it; ASP.NET Core resolves
// WebRootPath once at host build time, so the default UseStaticFiles() would bind to a
// NullFileProvider and 404 forever if wwwroot was missing at startup. Create the folder
// up front and point an explicit PhysicalFileProvider at it instead of relying on the
// WebRootPath convention.
var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRoot);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(webRoot) });
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply migrations + seed on startup; retry while MSSQL (docker) is still coming up.
await using (var scope = app.Services.CreateAsyncScope())
{
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

app.Run();
