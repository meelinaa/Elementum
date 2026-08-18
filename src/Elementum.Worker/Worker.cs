using Elementum.Application.Options;
using Elementum.Worker.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Worker;

/// <summary>
/// Hosted background daemon service that runs the metals price ingestion hourly (every <see cref="WorkerScheduleOptions.IngestionIntervalMinutes"/> minutes).
/// Also performs daily candle consolidation and 7-day retention cleanup.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _env;
    private readonly TimeSpan _interval;
    private bool _isFirstRun = true;

    /// <summary>Injects logger, scope factory, host environment, and schedule from configuration.</summary>
    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        IOptions<WorkerScheduleOptions> scheduleOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _interval = TimeSpan.FromMinutes(Math.Max(1, scheduleOptions.Value.IngestionIntervalMinutes));
    }

    /// <summary>Runs the ingestion daemon loop every hour.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elementum Ingestion Daemon started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_isFirstRun)
                {
                    _logger.LogInformation("Initial ingestion run starting immediately on boot...");
                    await RunJobAsync(stoppingToken);
                    _isFirstRun = false;
                }

                _logger.LogInformation("Waiting for next hourly run in {Interval}...", _interval);
                await Task.Delay(_interval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

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

    /// <summary>
    /// Resolves <see cref="MetalsIngestionJob"/> from a new scope and runs it once with exception shielding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
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
