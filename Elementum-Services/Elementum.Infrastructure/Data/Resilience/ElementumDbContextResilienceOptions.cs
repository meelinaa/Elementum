namespace Elementum.Infrastructure.Data.Resilience;

/// <summary>
/// Options for database resilience (retry on transient failures). Used by API and Worker when registering the DbContext with retry.
/// Bind via Configure(…) in AddElementumDbContext when <c>configureResilience</c> is provided.
/// </summary>
public record ElementumDbContextResilienceOptions
{
    /// <summary>Maximum number of retry attempts (including the first call). Default: 3.</summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>Initial delay before the first retry. Default: 1 second.</summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Use exponential backoff between retries (delay * 2^attempt). Default: true.</summary>
    public bool UseExponentialBackoff { get; init; } = true;
}
