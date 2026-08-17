using Elementum.Domain.Entities;
using Elementum.Domain.Models;
using Elementum.Domain.Ports;

namespace Elementum.Infrastructure.Data.Interfaces;

/// <summary>
/// Abstraction for Elementum data access. Extends the Domain Port <see cref="IPriceHistoryRepository"/>.
/// </summary>
public interface IElementumDbContext : IPriceHistoryRepository
{
}
