using Elementum.Application.Options;
using Elementum.Worker.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Worker;

/// <summary>
/// Hosted background daemon service that runs the metals price ingestion immediately on startup,
/// and on the configured schedule (<see cref="WorkerScheduleOptions.IngestionIntervalMinutes"/>).
/// Also performs daily candle consolidation and retention cleanup.
/// </summary>
public class Worker : BackgroundService
{
    private const int FullHourIntervalMinutes = 60;
    private static readonly TimeSpan BufferDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ErrorRetryDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MinimumFallbackDelay = TimeSpan.FromSeconds(1);

    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerScheduleOptions _options;

    /// <summary>Injects logger, scope factory, and worker schedule options.</summary>
    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerScheduleOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Runs the daemon: executes initial run immediately, then loops according to schedule.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elementum Ingestion Daemon initialized (Interval: {Interval}m, RollupHour: {Rollup}h, Retention: {Retention}d).",
            _options.IngestionIntervalMinutes, _options.DailyRollupHour, _options.RetentionDays);

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
            _logger.LogError(ex, "Error during initial ingestion on startup. Daemon will continue with scheduled loop.");
        }

        // 2. Loop at scheduled intervals
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetDelayUntilNextRun(out var nextRunUtc);
                _logger.LogInformation(
                    "Next ingestion scheduled for {NextRun:yyyy-MM-dd HH:mm:ss} UTC (in {Minutes:F1} minutes).",
                    nextRunUtc,
                    delay.TotalMinutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Starting scheduled ingestion at {Time:yyyy-MM-dd HH:mm:ss} UTC...", DateTime.UtcNow);
                await RunJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker daemon loop cancelled due to host shutdown.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in worker execution loop. Retrying in {Minutes} minute...", ErrorRetryDelay.TotalMinutes);
                try
                {
                    await Task.Delay(ErrorRetryDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private TimeSpan GetDelayUntilNextRun(out DateTime nextRunUtc)
    {
        if (_options.IngestionIntervalMinutes == FullHourIntervalMinutes)
        {
            return GetDelayUntilNextFullHour(out nextRunUtc);
        }

        var now = DateTime.UtcNow;
        nextRunUtc = now.AddMinutes(_options.IngestionIntervalMinutes);
        return TimeSpan.FromMinutes(_options.IngestionIntervalMinutes);
    }

    /// <summary>Calculates the time span remaining until the next top of the hour (XX:00:00 UTC).</summary>
    private static TimeSpan GetDelayUntilNextFullHour(out DateTime nextHourUtc)
    {
        var now = DateTime.UtcNow;
        nextHourUtc = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        var delay = nextHourUtc - now;
        return delay <= TimeSpan.Zero ? MinimumFallbackDelay : delay.Add(BufferDelay);
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
