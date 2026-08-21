namespace Elementum.Application.Requests;

/// <summary>
/// Query model for the optional <c>currency</c> parameter. Allowed values: USD or EUR.
/// </summary>
public record CurrencyRequest
{
    public string? Currency { get; init; }

    public CurrencyRequest() { }

    public CurrencyRequest(string? currency) => Currency = currency;
}
