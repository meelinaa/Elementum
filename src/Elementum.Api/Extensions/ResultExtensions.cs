using Elementum.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Extensions;

/// <summary>
/// Maps Domain <see cref="Result"/> and <see cref="Result{T}"/> outcomes to standard ASP.NET Core ActionResults and ProblemDetails.
/// </summary>
public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        return CreateProblemResult(result.Error);
    }

    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return CreateProblemResult(result.Error);
    }

    private static ObjectResult CreateProblemResult(Error error)
    {
        var statusCode = error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message,
            Type = statusCode == StatusCodes.Status404NotFound
                ? "https://tools.ietf.org/html/rfc7231#section-6.5.4"
                : "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }
}
