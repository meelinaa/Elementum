namespace Elementum.Application.Exceptions;

/// <summary>
/// Thrown when an external API response fails business expectations or yields empty data.
/// </summary>
public sealed class ExternalApiException : ApplicationException
{
    private ExternalApiException(string message) : base(message) { }

    public static ExternalApiException EmptyLiveQuoteResponse() =>
        new("External metals API returned an empty live quote response.");
}
