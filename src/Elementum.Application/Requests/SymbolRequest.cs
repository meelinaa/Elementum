namespace Elementum.Application.Requests;

/// <summary>
/// Request parameter model for queries filtering by metal symbol.
/// </summary>
public record SymbolRequest
{
    public string Symbol { get; init; } = string.Empty;
    public SymbolRequest() { }
    public SymbolRequest(string symbol) => Symbol = symbol;
}
