using Elementum.Api.Hosting;
using Serilog;

SerilogBootstrap.InitializeGlobalLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseElementumSerilog();
builder.Services.AddElementumApiServices(builder.Configuration);

var app = builder.Build();

app.UseElementumApiPipeline();

Log.Information("Elementum-ServiceApi started");
app.Run();

public partial class Program { }
