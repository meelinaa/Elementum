namespace Elementum.Application.Ports.Outbound;

/// <summary>
/// Outbound Port for recording poisoned / unparseable ingestion payloads into a Dead-Letter Queue (DLQ)
/// or telemetry sink for offline forensic inspection and alert triggers.
/// </summary>
public interface IIngestionDeadLetterSink
{
    /// <summary>
    /// Captures a malformed or rejected ingestion payload along with error details.
    /// </summary>
    /// <param name="rawPayload">The raw JSON/string payload from the upstream provider.</param>
    /// <param name="errorMessage">The validation or parsing error message.</param>
    /// <param name="source">Identifier of the upstream source (e.g. "EdelmetalleApi").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CapturePoisonPayloadAsync(
        string rawPayload,
        string errorMessage,
        string source = "EdelmetalleApi",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default no-op / fallback implementation of <see cref="IIngestionDeadLetterSink"/>.
/// </summary>
public sealed class NullIngestionDeadLetterSink : IIngestionDeadLetterSink
{
    public static readonly NullIngestionDeadLetterSink Instance = new();

    public Task CapturePoisonPayloadAsync(
        string rawPayload,
        string errorMessage,
        string source = "EdelmetalleApi",
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
