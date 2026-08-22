using System.Net;
using System.Text.Json;
using Elementum.IntegrationTests.Fixtures;

namespace Elementum.IntegrationTests.Api;

public class OpenApiErrorSchemaTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OpenApiErrorSchemaTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // [R]IGHT-BICEP: generated OpenAPI document describes 400/404/429 ProblemDetails the same way API-Endpoints.md does
    [Fact]
    public async Task OpenApiDocument_IncludesProblemDetailsErrorResponses()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        var live = Operation(paths, "/api/v1/prices/live", "get");
        AssertStatus(live, "429", "rate limit");
        Assert.False(live.GetProperty("responses").TryGetProperty("400", out _));
        Assert.False(live.GetProperty("responses").TryGetProperty("404", out _));

        var trading = Operation(paths, "/api/v1/prices/live/trading/{symbol}", "get");
        AssertStatus(trading, "400", "validation");
        AssertStatus(trading, "404", "symbol");
        AssertStatus(trading, "429", "rate limit");
        Assert.Contains("rfc7231#section-6.5.4", ExampleJson(trading, "404"), StringComparison.Ordinal);
        Assert.Contains("rfc9110#section-15.5.20", ExampleJson(trading, "429"), StringComparison.Ordinal);

        var history = Operation(paths, "/api/v1/history/{symbol}", "get");
        AssertStatus(history, "400", "validation");
        AssertStatus(history, "429", "rate limit");
        Assert.False(history.GetProperty("responses").TryGetProperty("404", out _));
        Assert.Contains("rfc7231#section-6.5.1", ExampleJson(history, "400"), StringComparison.Ordinal);
    }

    private static JsonElement Operation(JsonElement paths, string path, string method)
    {
        Assert.True(paths.TryGetProperty(path, out var item), $"OpenAPI paths missing '{path}'.");
        Assert.True(item.TryGetProperty(method, out var operation), $"OpenAPI missing {method.ToUpperInvariant()} {path}.");
        return operation;
    }

    private static void AssertStatus(JsonElement operation, string status, string descriptionFragment)
    {
        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty(status, out var response), $"Missing {status} response.");
        var description = response.GetProperty("description").GetString() ?? string.Empty;
        Assert.Contains(descriptionFragment, description, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            response.GetProperty("content").TryGetProperty("application/problem+json", out var media),
            $"{status} must be documented as application/problem+json.");
        Assert.True(media.TryGetProperty("schema", out _), $"{status} must include a schema.");
        Assert.True(media.TryGetProperty("example", out _), $"{status} must include an example payload.");
    }

    private static string ExampleJson(JsonElement operation, string status)
    {
        var example = operation
            .GetProperty("responses")
            .GetProperty(status)
            .GetProperty("content")
            .GetProperty("application/problem+json")
            .GetProperty("example");
        return example.GetRawText();
    }
}
