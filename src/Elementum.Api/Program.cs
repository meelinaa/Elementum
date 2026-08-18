using Elementum.Api.Hosting;
using Elementum.Infrastructure.Outbound.Data;
using Serilog;

SerilogBootstrap.InitializeGlobalLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseElementumSerilog();
builder.Services.AddElementumApiServices(builder.Configuration);

var app = builder.Build();

await app.Services.ApplyMigrationsAndSeedAsync();

app.UseElementumApiPipeline();

Log.Information("Elementum-ServiceApi started");
app.Run();