using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using VoltElectronics.Infrastructure.Auth;

namespace VoltElectronics.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>HTTP-edge concerns: JWT validation, CORS, JSON shape, Swagger.</summary>
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services
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
        services.AddAuthorization();

        services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
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

        var corsOrigins = config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];
        services.AddCors(o => o.AddPolicy("frontend", p =>
            p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

        return services;
    }
}
