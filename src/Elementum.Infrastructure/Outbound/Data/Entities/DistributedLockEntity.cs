namespace Elementum.Infrastructure.Outbound.Data.Entities;

/// <summary>
/// Infrastructure persistence entity representing a distributed lock record managed via Entity Framework Core.
/// </summary>
public class DistributedLockEntity
{
    /// <summary>Unique resource key (e.g. "lock:job:metals_ingestion").</summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>Identifier of the machine, container, or process holding the lock.</summary>
    public string AcquiredBy { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the lock was acquired.</summary>
    public DateTime AcquiredAtUtc { get; set; }

    /// <summary>UTC timestamp when the lock expires (TTL for crash recovery). Persistence maps this as a concurrency token.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}
