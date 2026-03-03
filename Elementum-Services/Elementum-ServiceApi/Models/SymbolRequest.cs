using System.ComponentModel.DataAnnotations;

namespace Elementum_ServiceApi.Models
{
    public class SymbolRequest
    {
        [Required(ErrorMessage = "Symbol is required and cannot be empty.")]
        [MinLength(1, ErrorMessage = "Symbol cannot be empty.")]
        public string Symbol { get; set; } = string.Empty;
    }
}
