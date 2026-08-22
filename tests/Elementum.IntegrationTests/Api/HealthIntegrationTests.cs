using System.Net;
using System.Text.Json;
using Elementum.IntegrationTests.Fixtures;

namespace Elementum.IntegrationTests.Api;

public class HealthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // [R]IGHT-BICEP: liveness probe is unversioned, skips DB checks, and reports process-up
    [Fact]
    public async Task Live_Returns200Healthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // [R]IGHT-BICEP: readiness probe includes the tagged database check as JSON
    [Fact]
    public async Task Ready_Returns200WithHealthyDatabaseCheck()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", doc.RootElement.GetProperty("Status").GetString());

        var checks = doc.RootElement.GetProperty("Checks");
        Assert.True(checks.GetArrayLength() >= 1);
        Assert.Contains(
            checks.EnumerateArray(),
            check => check.GetProperty("Component").GetString() == "database"
                     && check.GetProperty("Status").GetString() == "Healthy");
    }
}
