using Elementum.Domain.Constants;

namespace Elementum.Application.Requests;

/// <summary>
/// Validated query model for the optional <c>currency</c> parameter. Allowed values: USD or EUR.
/// </summary>
public record CurrencyRequest
{
    public string? Currency { get; init; }

    public CurrencyRequest() { }

    public CurrencyRequest(string? currency) => Currency = currency;

    /// <summary>Trimmed ISO code, or <c>null</c> when omitted (history returns all currencies).</summary>
    public string? NormalizedOrNull() => Normalize(Currency);

    /// <summary>Trading defaults to EUR when the query parameter is omitted.</summary>
    public string ForTrading() => NormalizedOrNull() ?? DomainConstants.Currencies.Eur;

    internal static string? Normalize(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant();
}
