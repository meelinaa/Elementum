using Polly;

namespace Elementum.Infrastructure.Data.Resilience;

/// <summary>
/// Shared database resilience: defines which exceptions are transient and builds a Polly retry policy.
/// Used by API and Worker when registering the DbContext with retry (see AddElementumDbContext overload with configureResilience).
/// </summary>
public static class DatabaseResiliencePolicy
{
    /// <summary>
    /// Returns true if the exception is considered transient and worth retrying (e.g. connection lost, deadlock).
    /// Uses MySqlConnector's <see cref="MySqlConnector.MySqlException.IsTransient"/>; does not retry on timeout or cancellation.
    /// </summary>
    public static bool IsTransientException(Exception ex)
    {
        if (ex is MySqlConnector.MySqlException mySql && mySql.IsTransient)
            return true;
        if (ex is TimeoutException or OperationCanceledException)
            return false;
        if (ex.InnerException != null)
            return IsTransientException(ex.InnerException);
        return false;
    }

    /// <summary>
    /// Builds a Polly retry policy from <paramref name="options"/>. Used when registering <see cref="ResilientElementumDbContext"/> (API or Worker).
    /// </summary>
    public static IAsyncPolicy BuildRetryPolicy(ElementumDbContextResilienceOptions options)
    {
        // Delay after each failed attempt: exponential (delay * 2^attempt) or constant.
        Func<int, TimeSpan> delay = options.UseExponentialBackoff
            ? (int attempt) => TimeSpan.FromMilliseconds(options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt))
            : (int _) => options.InitialDelay;

        return Policy
            .Handle<Exception>(IsTransientException)
            .WaitAndRetryAsync(
                options.MaxRetryCount,
                delay,
                onRetry: (_, timeSpan, attempt, _) => { /* optional: log retry */ });
    }
}
