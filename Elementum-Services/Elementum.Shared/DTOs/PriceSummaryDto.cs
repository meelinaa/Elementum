namespace Elementum.Shared.DTOs;

/// <summary>
/// Minimal DTO for dashboard / "latest price per metal" lists. Only symbol, name, exchange, price, change percent.
/// </summary>
public class PriceSummaryDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public decimal Price { get; set; }
    public decimal? Chp { get; set; }
    public DateOnly EntryDate { get; set; }
}
