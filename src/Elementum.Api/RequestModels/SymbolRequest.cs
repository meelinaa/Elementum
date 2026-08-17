using System.ComponentModel.DataAnnotations;

namespace Elementum.Api.RequestModels
{
    /// <summary>
    /// Request model for route parameter <c>symbol</c> (e.g. XAU, XAG, XPT). Validated via DataAnnotations.
    /// </summary>
    public class SymbolRequest
    {
        [Required(ErrorMessage = "Symbol is required and cannot be empty.")]
        [MinLength(1, ErrorMessage = "Symbol cannot be empty.")]
        public string Symbol { get; set; } = string.Empty;
    }
}
