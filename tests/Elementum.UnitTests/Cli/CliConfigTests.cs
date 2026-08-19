using Elementum.Cli.Config;

namespace Elementum.Cli.Tests;

public class CliConfigTests
{
    // [R]IGHT-BICEP & [B]OUNDARY: Verifies that whitespace is trimmed and trailing slashes are consistently ensured
    [Theory]
    [InlineData("  http://example.com/api/v1  ", "http://example.com/api/v1/")]
    [InlineData("http://example.com/api/v1/", "http://example.com/api/v1/")]
    [InlineData("http://example.com/api/v1////", "http://example.com/api/v1/")]
    public void NormalizeBaseUrl_TrimsAndEnsuresTrailingSlash(string input, string expected)
    {
        // Arrange & Act
        var normalized = CliConfig.NormalizeBaseUrl(input);

        // Assert
        Assert.Equal(expected, normalized);
    }

    // [R]IGHT-BICEP: Verifies that environment variable setting takes priority over file configuration
    [Fact]
    public void ResolveApiBaseUrl_EnvOverridesFile()
    {
        // Arrange
        const string envUrl = "http://env/api";
        const string fileUrl = "http://file/api";

        // Act
        var resolved = CliConfig.ResolveApiBaseUrl(envUrl, fileUrl);

        // Assert
        Assert.Equal("http://env/api/", resolved);
    }

    // [B]OUNDARY: Verifies fallback to configuration file when environment variable is null
    [Fact]
    public void ResolveApiBaseUrl_UsesFileWhenEnvMissing()
    {
        // Arrange
        const string fileUrl = "http://file/api";

        // Act
        var resolved = CliConfig.ResolveApiBaseUrl(null, fileUrl);

        // Assert
        Assert.Equal("http://file/api/", resolved);
    }
}
