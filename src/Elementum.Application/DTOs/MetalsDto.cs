namespace Elementum.Application.DTOs;

public record MetalsDto
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
