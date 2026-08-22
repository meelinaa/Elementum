using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Outbound.Caching;
using Elementum.Infrastructure.Outbound.Data.Resilience;
using Elementum.Worker.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum.UnitTests.Worker.Hosting;

public class WorkerHostBuilderExtensionsTests
{
    // [R]IGHT-BICEP: Worker DI matches the API — Polly decorator plus Redis L2 from ConnectionStrings:Redis
    [Fact]
    public void AddWorkerApplicationServices_WhenRedisConfigured_WiresResilienceAndRedisCache()
    {
        using var provider = BuildWorkerServices(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Port=1;Database=elementum;User=root;Password=x;",
            ["ConnectionStrings:Redis"] = "localhost:6379",
            ["MetalsApi:BaseUrl"] = "https://example.test/public.json",
            ["MetalsApi:Currency"] = "USD",
            ["WorkerSchedule:IngestionIntervalMinutes"] = "60",
            ["WorkerSchedule:DailyRollupHour"] = "22",
            ["WorkerSchedule:RetentionDays"] = "7"
        });

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPriceHistoryRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var history = scope.ServiceProvider.GetRequiredService<IGetPriceHistoryUseCase>();

        Assert.IsType<ResilientElementumDbContext>(db);
        Assert.Contains("RedisCache", cache.GetType().Name, StringComparison.Ordinal);
        Assert.IsType<CachedGetPriceHistoryUseCase>(history);
    }

    // [R]IGHT-BICEP: REDIS_CONNECTION_STRING is the same fallback the API uses
    [Fact]
    public void AddWorkerApplicationServices_WhenRedisEnvFallback_RegistersDistributedCache()
    {
        using var provider = BuildWorkerServices(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Port=1;Database=elementum;User=root;Password=x;",
            ["REDIS_CONNECTION_STRING"] = "redis:6379",
            ["MetalsApi:BaseUrl"] = "https://example.test/public.json",
            ["MetalsApi:Currency"] = "USD",
            ["WorkerSchedule:IngestionIntervalMinutes"] = "60",
            ["WorkerSchedule:DailyRollupHour"] = "22",
            ["WorkerSchedule:RetentionDays"] = "7"
        });

        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        Assert.Contains("RedisCache", cache.GetType().Name, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildWorkerServices(Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.AddWorkerApplicationServices();
        return builder.Services.BuildServiceProvider();
    }
}
