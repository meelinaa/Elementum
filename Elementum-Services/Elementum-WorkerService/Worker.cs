using Elementum_WorkerService.Jobs;

namespace Elementum_WorkerService;

/// <summary>
/// Hosted background service that runs the metals ingestion job on a daily schedule (default 23:00).
/// In development, the first run is executed immediately; subsequent runs wait until the next scheduled time.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _env;
    private readonly TimeSpan _runTime = new(23, 0, 0);
    private bool _isFirstRun = true;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IHostEnvironment env)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_isFirstRun && _env.IsDevelopment())
            {
                _logger.LogInformation("Debug mode detected: Start first run immediately...");
                await RunJobAsync(stoppingToken);
                _isFirstRun = false;
            }

            var now = DateTime.Now;
            var nextRun = now.Date.Add(_runTime);
            if (now > nextRun)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogInformation("Next regular run: {NextRun}", nextRun);
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunJobAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Resolves <see cref="MetalsIngestionJob"/> from a new scope and runs it once.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task RunJobAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<MetalsIngestionJob>();
        await job.RunAsync(cancellationToken);
    }
}
