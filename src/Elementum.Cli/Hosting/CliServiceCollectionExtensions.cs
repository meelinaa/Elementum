using Elementum.Cli.Api;
using Elementum.Cli.Config;
using Elementum.Cli.Views;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Hosting;

/// <summary>
/// Registers CLI services: logging, memory cache, named HttpClient with timeout, and views.
/// </summary>
public static class CliServiceCollectionExtensions
{
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddElementumCli();
        return services.BuildServiceProvider();
    }

    public static IServiceCollection AddElementumCli(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddMemoryCache();

        var baseUrl = CliConfig.ApiBaseUrl;
        services.AddHttpClient(nameof(HttpCall), client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = HttpCall.DefaultTimeout;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        // One instance for the CLI session so cache and ClearCache stay shared across views.
        services.AddSingleton<IHttpCall>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(HttpCall));
            return new HttpCall(
                http,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<HttpCall>>());
        });
        services.AddSingleton<ViewRegistry>();

        return services;
    }
}
