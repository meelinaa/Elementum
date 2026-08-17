namespace Elementum.Domain.Ports;

/// <summary>
/// Secondary / Driven Port: Checks database connectivity and health.
/// </summary>
public interface IDatabaseCheckService
{
    /// <summary>Tests database connection.</summary>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
