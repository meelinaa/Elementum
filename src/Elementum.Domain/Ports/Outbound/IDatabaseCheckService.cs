namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Secondary / Driven Outbound Port: Checks database connectivity and health.
/// </summary>
public interface IDatabaseCheckService
{
    /// <summary>Tests database connection.</summary>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
