using Elementum.Worker.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elementum.Worker;

/// <summary>
/// Hosted background daemon service that runs the metals price ingestion immediately on startup,
/// and then at every full hour (XX:00:00 UTC).
/// Also performs daily candle consolidation and 7-day retention cleanup.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Injects logger and scope factory.</summary>
    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <summary>Runs the daemon: executes initial run immediately, then loops every full hour.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elementum Ingestion Daemon initialized.");

        // 1. Initial run immediately on startup
        try
        {
            _logger.LogInformation("Executing initial precious metals ingestion on startup...");
            await RunJobAsync(stoppingToken);
            _logger.LogInformation("Initial startup ingestion completed successfully.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker daemon stopped during initial run.");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial ingestion on startup. Daemon will continue with hourly schedule.");
        }

        // 2. Loop at every full hour
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetDelayUntilNextFullHour(out var nextHourUtc);
                _logger.LogInformation(
                    "Next hourly ingestion scheduled for {NextHour:yyyy-MM-dd HH:00:00} UTC (in {Minutes:F1} minutes).",
                    nextHourUtc,
                    delay.TotalMinutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Starting scheduled hourly ingestion at {Time:yyyy-MM-dd HH:mm:ss} UTC...", DateTime.UtcNow);
                await RunJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker daemon loop cancelled due to host shutdown.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in worker execution loop. Retrying in 1 minute...");
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    /// <summary>Calculates the time span remaining until the next top of the hour (XX:00:00 UTC).</summary>
    private static TimeSpan GetDelayUntilNextFullHour(out DateTime nextHourUtc)
    {
        var now = DateTime.UtcNow;
        nextHourUtc = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        var delay = nextHourUtc - now;
        // Add a 500ms safety buffer so we are strictly in the new hour
        return delay <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : delay.Add(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Resolves <see cref="MetalsIngestionJob"/> from a new scope and runs it once with exception shielding.
    /// </summary>
    private async Task RunJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<MetalsIngestionJob>();
            await job.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Metals ingestion job cancelled as host is shutting down.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during metals ingestion job execution. Host process remains running.");
        }
    }
}
