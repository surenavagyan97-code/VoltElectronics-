using VoltElectronics.Api.Endpoints;
using VoltElectronics.Api.Extensions;
using VoltElectronics.Api.Middleware;
using VoltElectronics.Application;
using VoltElectronics.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseUploadedFiles();
app.UseCors("frontend");
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapVoltEndpoints();

await app.MigrateAndSeedAsync();

app.Run();
