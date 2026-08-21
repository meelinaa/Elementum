namespace Elementum.Application.Requests;

/// <summary>
/// Query model for the optional <c>currency</c> parameter (USD or EUR).
/// </summary>
public record CurrencyRequest
{
    public string? Currency { get; init; }
}
