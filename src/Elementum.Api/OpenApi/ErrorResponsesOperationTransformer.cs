using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Elementum.Api.OpenApi;

/// <summary>
/// Adds RFC 7807 error responses (400/404/429) to generated operations so the OpenAPI document
/// matches the examples in <c>API-Endpoints.md</c>.
/// </summary>
internal sealed class ErrorResponsesOperationTransformer : IOpenApiOperationTransformer
{
    private const string ProblemJson = "application/problem+json";

    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var path = context.Description.RelativePath ?? string.Empty;
        var problemSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
        var validationSchema = await context.GetOrCreateSchemaAsync(typeof(ValidationProblemDetails), cancellationToken: cancellationToken);

        var hasValidatedInput =
            path.Contains("history", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("trading", StringComparison.OrdinalIgnoreCase);

        if (hasValidatedInput)
        {
            AddResponse(
                operation,
                "400",
                "Validation failed (symbol, currency, or history query).",
                validationSchema,
                """
                {
                  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                  "title": "One or more validation errors occurred.",
                  "status": 400,
                  "errors": {
                    "symbol": ["The Symbol field is required."]
                  }
                }
                """);
        }

        if (path.Contains("prices/live/trading", StringComparison.OrdinalIgnoreCase))
        {
            AddResponse(
                operation,
                "404",
                "No trading analysis for the given symbol.",
                problemSchema,
                """
                {
                  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                  "title": "Resource not found",
                  "status": 404,
                  "detail": "No trading analysis found for symbol 'NONEXISTENT'.",
                  "instance": "GET /api/v1/prices/live/trading/NONEXISTENT"
                }
                """);
        }

        AddResponse(
            operation,
            "429",
            "IP rate limit exceeded.",
            problemSchema,
            """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.20",
              "title": "Too Many Requests",
              "status": 429,
              "detail": "Rate limit exceeded. Please try again later.",
              "instance": "GET /api/v1/prices/live"
            }
            """);
    }

    private static void AddResponse(
        OpenApiOperation operation,
        string statusCode,
        string description,
        IOpenApiSchema schema,
        string exampleJson)
    {
        operation.Responses ??= [];
        if (operation.Responses.ContainsKey(statusCode))
            return;

        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [ProblemJson] = new OpenApiMediaType
                {
                    Schema = schema,
                    Example = JsonNode.Parse(exampleJson)
                }
            }
        };
    }
}
