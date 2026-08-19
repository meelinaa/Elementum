using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Data.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Services;

/// <summary>
/// Secondary / Driven Adapter: Verifies MySQL database connection.
/// Uses <see cref="DatabaseLogMessages"/> for zero-allocation logging.
/// </summary>
public class DatabaseCheckService : IDatabaseCheckService
{
    private readonly ILogger<DatabaseCheckService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseCheckService(ILogger<DatabaseCheckService> logger, IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopeFactory);

        _logger = logger;
        _scopeFactory = scopeFactory;
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
            DatabaseLogMessages.DatabaseCheckFailed(_logger, ex);
            return false;
        }
    }
}
