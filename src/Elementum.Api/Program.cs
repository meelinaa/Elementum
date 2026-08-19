using Elementum.Api.Hosting;
using Elementum.Api.Logging;
using Elementum.Infrastructure.Outbound.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

SerilogBootstrap.InitializeGlobalLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseElementumSerilog();
builder.Services.AddElementumApiServices(builder.Configuration);

var app = builder.Build();

await app.Services.ApplyMigrationsAndSeedAsync();

app.UseElementumApiPipeline();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
ApiLogMessages.ApiServiceStarted(logger);

app.Run();