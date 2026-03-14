namespace Elementum.Shared.Objects;

/// <summary>
/// Entity for the <c>metals</c> table (Gold, Silver, Platinum). Used by EF Core and mapped to <see cref="DTOs.MetalsDto"/> for API responses.
/// </summary>
public class Metals
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}
