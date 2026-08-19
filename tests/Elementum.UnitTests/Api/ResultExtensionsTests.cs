using Elementum.Api.Extensions;
using Elementum.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.UnitTests.Api;

public class ResultExtensionsTests
{
    // [R]IGHT-BICEP: Verifies that parameterless successful Result maps to HTTP 200 OkResult
    [Fact]
    public void ToActionResult_ResultSuccess_ReturnsOkResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        Assert.IsType<OkResult>(actionResult);
    }

    // [E]RROR: Verifies that NotFound error result maps to HTTP 404 ProblemDetails
    [Fact]
    public void ToActionResult_ResultFailureNotFound_Returns404ProblemDetails()
    {
        // Arrange
        var result = Result.Failure(new Error("Metal.NotFound", "Metal not found"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var obj = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("Metal not found", problem.Detail);
    }

    // [E]RROR: Verifies that generic validation failure error result maps to HTTP 400 ProblemDetails
    [Fact]
    public void ToActionResult_ResultFailureBadRequest_Returns400ProblemDetails()
    {
        // Arrange
        var result = Result.Failure(new Error("InvalidInput", "Invalid date format"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var obj = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("Invalid date format", problem.Detail);
    }

    // [R]IGHT-BICEP: Verifies that typed successful Result<T> maps to HTTP 200 OkObjectResult carrying the value
    [Fact]
    public void ToActionResult_GenericResultSuccess_ReturnsOkObjectResultWithValue()
    {
        // Arrange
        var result = Result.Success("TestValue");

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal("TestValue", ok.Value);
    }

    // [E]RROR: Verifies that typed failed Result<T> maps to ProblemDetails with matching status code
    [Fact]
    public void ToActionResult_GenericResultFailure_ReturnsProblemDetails()
    {
        // Arrange
        var result = Result.Failure<string>(new Error("Item.NotFound", "Item was not found"));

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        var obj = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }
}
