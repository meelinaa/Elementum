using Elementum.Application.Options;
using Elementum.Worker.Jobs;
using Elementum.Worker.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Worker;

/// <summary>
/// Hosted background daemon service that runs the metals price ingestion immediately on startup,
/// and on the configured schedule (<see cref="WorkerScheduleOptions.IngestionIntervalMinutes"/>).
/// Also performs daily candle consolidation and retention cleanup.
/// Uses zero-allocation, source-generated <see cref="WorkerLogMessages"/>.
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
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Value);

        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    /// <summary>Runs the daemon: executes initial run immediately, then loops according to schedule.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        WorkerLogMessages.DaemonInitialized(_logger, _options.IngestionIntervalMinutes, _options.DailyRollupHour, _options.RetentionDays);

        // 1. Initial run immediately on startup
        try
        {
            WorkerLogMessages.InitialIngestionStarting(_logger);
            await RunJobAsync(stoppingToken);
            WorkerLogMessages.InitialIngestionCompleted(_logger);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            WorkerLogMessages.WorkerDaemonStopped(_logger);
            return;
        }
        catch (Exception ex)
        {
            WorkerLogMessages.InitialIngestionFailed(_logger, ex);
        }

        // 2. Loop at scheduled intervals
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetDelayUntilNextRun(out var nextRunUtc);
                WorkerLogMessages.ScheduledCycleWait(_logger, delay.TotalMinutes, nextRunUtc);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                WorkerLogMessages.ScheduledIngestionStarting(_logger, DateTime.UtcNow);
                await RunJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                WorkerLogMessages.WorkerDaemonCancelled(_logger);
                break;
            }
            catch (Exception ex)
            {
                WorkerLogMessages.WorkerLoopError(_logger, ErrorRetryDelay.TotalMinutes, ex);
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
            WorkerLogMessages.JobCancelledHostShutdown(_logger);
        }
        catch (Exception ex)
        {
            WorkerLogMessages.JobUnhandledError(_logger, ex);
        }
    }
}
