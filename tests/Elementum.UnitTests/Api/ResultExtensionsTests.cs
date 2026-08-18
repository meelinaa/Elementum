using Elementum.Api.Extensions;
using Elementum.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.UnitTests.Api;

public class ResultExtensionsTests
{
    [Fact]
    public void ToActionResult_ResultSuccess_ReturnsOkResult()
    {
        var result = Result.Success();
        var actionResult = result.ToActionResult();

        Assert.IsType<OkResult>(actionResult);
    }

    [Fact]
    public void ToActionResult_ResultFailureNotFound_Returns404ProblemDetails()
    {
        var result = Result.Failure(new Error("Metal.NotFound", "Metal not found"));
        var actionResult = result.ToActionResult();

        var obj = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("Metal not found", problem.Detail);
    }

    [Fact]
    public void ToActionResult_ResultFailureBadRequest_Returns400ProblemDetails()
    {
        var result = Result.Failure(new Error("InvalidInput", "Invalid date format"));
        var actionResult = result.ToActionResult();

        var obj = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("Invalid date format", problem.Detail);
    }

    [Fact]
    public void ToActionResult_GenericResultSuccess_ReturnsOkObjectResultWithValue()
    {
        var result = Result.Success("TestValue");
        var actionResult = result.ToActionResult();

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal("TestValue", ok.Value);
    }

    [Fact]
    public void ToActionResult_GenericResultFailure_ReturnsProblemDetails()
    {
        var result = Result.Failure<string>(new Error("Item.NotFound", "Item was not found"));
        var actionResult = result.ToActionResult();

        var obj = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }
}
