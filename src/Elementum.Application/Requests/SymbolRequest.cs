using System.ComponentModel.DataAnnotations;

namespace Elementum.Application.Requests;

/// <summary>
/// Request parameter model for queries filtering by metal symbol.
/// </summary>
public record SymbolRequest
{
    [Required(ErrorMessage = "Symbol is required and cannot be empty.")]
    [MinLength(2, ErrorMessage = "Symbol must be at least 2 characters long.")]
    [MaxLength(15, ErrorMessage = "Symbol cannot exceed 15 characters.")]
    [RegularExpression(@"^[A-Za-z0-9_:\-]+$", ErrorMessage = "Symbol must be a valid alphanumeric identifier (e.g. XAU, XAG, XPT, XPD).")]
    public string Symbol { get; init; } = string.Empty;

    public SymbolRequest() { }
    public SymbolRequest(string symbol) => Symbol = symbol;
}
