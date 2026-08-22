using System.ComponentModel.DataAnnotations;

namespace Elementum.Application.Options;

/// <summary>
/// Configuration options for API rate limiting.
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Maximum number of permit requests allowed within the time window.</summary>
    [Required]
    [Range(1, 100000, ErrorMessage = "PermitLimit must be between 1 and 100,000.")]
    public int PermitLimit { get; set; } = 100;

    /// <summary>Time window length in seconds.</summary>
    [Required]
    [Range(1, 86400, ErrorMessage = "WindowSeconds must be between 1 and 86,400 (24h).")]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Maximum number of requests allowed in the backlog queue when limit is reached.</summary>
    [Required]
    [Range(0, 1000, ErrorMessage = "QueueLimit must be between 0 and 1,000.")]
    public int QueueLimit { get; set; } = 0;
}
