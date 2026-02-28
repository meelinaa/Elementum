using Elementum.Shared.Helpers;
using Elementum_WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<OutputHelper>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
