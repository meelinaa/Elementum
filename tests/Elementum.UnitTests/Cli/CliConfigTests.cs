using Elementum.Cli.Config;

namespace Elementum.Cli.Tests;

public class CliConfigTests
{
    [Fact]
    public void NormalizeBaseUrl_TrimsAndEnsuresTrailingSlash()
    {
        CliConfig.NormalizeBaseUrl("  http://example.com/api/v1  ").Should().Be("http://example.com/api/v1/");
        CliConfig.NormalizeBaseUrl("http://example.com/api/v1/").Should().Be("http://example.com/api/v1/");
        CliConfig.NormalizeBaseUrl("http://example.com/api/v1////").Should().Be("http://example.com/api/v1/");
    }

    [Fact]
    public void ResolveApiBaseUrl_EnvOverridesFile()
    {
        var resolved = CliConfig.ResolveApiBaseUrl("http://env/api", "http://file/api");
        resolved.Should().Be("http://env/api/");
    }

    [Fact]
    public void ResolveApiBaseUrl_UsesFileWhenEnvMissing()
    {
        var resolved = CliConfig.ResolveApiBaseUrl(null, "http://file/api");
        resolved.Should().Be("http://file/api/");
    }
}

internal static class FluentAssertionsLite
{
    public static StringAssertions Should(this string actual) => new(actual);
}

internal readonly record struct StringAssertions(string Actual)
{
    public void Be(string expected) => Assert.Equal(expected, Actual);
}

