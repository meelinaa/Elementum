namespace Elementum.Domain.Entities;

public record Metals
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime? CreatedAt { get; init; }
    public List<PriceHistory> PriceHistory { get; init; } = new();
}
