namespace Elementum.Cli.Exceptions;

/// <summary>
/// Thrown when required CLI configuration settings are missing.
/// </summary>
public sealed class CliConfigurationException : CliException
{
    private CliConfigurationException(string message) : base(message) { }

    public static CliConfigurationException MissingApiBaseUrl() =>
        new("ApiBaseUrl configuration is missing. Configure 'ApiBaseUrl' in appsettings.json or set the 'ELEMENTUM_API_BASEURL' environment variable.");
}
