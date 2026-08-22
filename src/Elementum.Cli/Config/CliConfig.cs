using Elementum.Cli.Exceptions;
using Elementum.Cli.Logging;
using Microsoft.Extensions.Configuration;

namespace Elementum.Cli.Config;

/// <summary>
/// CLI configuration. Base URL: env ELEMENTUM_API_BASEURL overrides appsettings.json.
/// Fail-fast: Throws <see cref="CliConfigurationException"/> if URL is missing.
/// </summary>
public static class CliConfig
{
    private const string EnvApiBaseUrl = "ELEMENTUM_API_BASEURL";

    private static readonly Lazy<string> _baseUrl = new(LoadBaseUrl);

    /// <summary>API base URL for HTTP calls. Set ELEMENTUM_API_BASEURL to override.</summary>
    public static string ApiBaseUrl => _baseUrl.Value;

    /// <summary>Trims the URL and ensures it ends with a single trailing slash.</summary>
    public static string NormalizeBaseUrl(string baseUrl)
        => baseUrl.Trim().TrimEnd('/') + "/";

    /// <summary>Returns the API base URL: env overrides file. Throws if neither is configured.</summary>
    public static string ResolveApiBaseUrl(string? fromEnv, string? fromFile)
    {
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return NormalizeBaseUrl(fromEnv);
        if (!string.IsNullOrWhiteSpace(fromFile))
            return NormalizeBaseUrl(fromFile);

        throw CliConfigurationException.MissingApiBaseUrl();
    }

    private static string LoadBaseUrl()
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvApiBaseUrl);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return NormalizeBaseUrl(fromEnv);
        }

        string? fromFile = null;
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var config = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            fromFile = config["ApiBaseUrl"];
        }
        catch (Exception ex)
        {
            CliLogMessages.ConfigLoadFailed(CliLogging.GetLogger(nameof(CliConfig)), ex);
            throw;
        }

        return ResolveApiBaseUrl(fromEnv, fromFile);
    }
}
