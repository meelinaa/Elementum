namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Composite Outbound Port: Combines <see cref="IPriceHistoryReadRepository"/> and <see cref="IPriceHistoryWriteRepository"/>.
/// Maintained for backwards-compatibility and components requiring full persistence access.
/// </summary>
public interface IPriceHistoryRepository : IPriceHistoryReadRepository, IPriceHistoryWriteRepository
{
}
