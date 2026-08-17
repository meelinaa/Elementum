namespace Elementum.Application.UseCases.Ingestion;

/// <summary>
/// Primary / Inbound Port: Use case for ingesting daily precious metal prices.
/// </summary>
public interface IIngestPricesUseCase
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
