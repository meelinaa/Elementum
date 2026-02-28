using Elementum.Shared.Helpers;

namespace Elementum_WorkerService;

public class Worker(OutputHelper output, IHostEnvironment env) : BackgroundService
{
    private readonly TimeSpan _runTime = new(12, 0, 0); // 12:00
    private bool _isFirstRun = true; // Flag for the initial start

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // DEBUG: Start immediately the first time in development mode
            if (_isFirstRun && env.IsDevelopment())
            {
                output.WriteLine("Debug mode detected: Start first run immediately...");
                await DoWorkAsync();
                _isFirstRun = false; // Then back to the normal rhythm
            }

            var now = DateTime.Now;
            var nextRun = now.Date.Add(_runTime);

            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            output.Info($"Next regular run: {nextRun}");

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync();
            }
        }
    }

    private async Task DoWorkAsync()
    {
        output.WriteLine("Work is being done...");
        // 1. get Request to the API



        // 2. process the Request
        // 3. save the result to the database
        await Task.Delay(1000);
    }
}