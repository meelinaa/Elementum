using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Services;

/// <summary>
/// Secondary / Driven Adapter: Verifies MySQL database connection.
/// </summary>
public class DatabaseCheckService : IDatabaseCheckService
{
    private readonly ILogger<DatabaseCheckService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseCheckService(ILogger<DatabaseCheckService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            return await db.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection check failed.");
            return false;
        }
    }
}
