namespace Elementum.Application.Exceptions;

/// <summary>
/// Thrown when required environment or application settings are missing or misconfigured.
/// </summary>
public sealed class ConfigurationException : ApplicationException
{
    private ConfigurationException(string message) : base(message) { }

    public static ConfigurationException MissingConnectionString(string key) =>
        new($"Configure ConnectionStrings:{key} or {key.ToUpperInvariant()} in appsettings.json or environment variables.");
}
