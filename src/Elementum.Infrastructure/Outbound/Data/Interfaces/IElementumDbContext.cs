using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;

namespace Elementum.Infrastructure.Outbound.Data.Interfaces;

/// <summary>
/// Abstraction for Elementum data access. Extends the Domain Port <see cref="IPriceHistoryRepository"/>.
/// </summary>
public interface IElementumDbContext : IPriceHistoryRepository
{
}
