using Elementum.Domain.Entities;

namespace Elementum.UnitTests.Architecture.Probes;

/// <summary>
/// Deliberate IQueryable leak. Used by
/// <see cref="Elementum.UnitTests.Architecture.LayerDependencyTests.QueryablePassthroughProbe_IsReportedAsDeferredQuery"/>.
/// Must not exist on production ports or <c>ResilientElementumDbContext</c>.
/// </summary>
public sealed class QueryablePassthroughProbe
{
    public IQueryable<PriceHistory> QueryPriceHistoryAll() => throw new NotSupportedException();
}
