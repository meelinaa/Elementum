namespace Elementum.Shared.DTOs;

/// <summary>
/// Slim DTO for metal list/detail API responses. Excludes internal fields like CreatedAt.
/// </summary>
public class MetalsDto
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
