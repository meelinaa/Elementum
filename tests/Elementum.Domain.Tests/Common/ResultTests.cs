using Elementum.Domain.Common;

namespace Elementum.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_CreatesFailedResult_WithError()
    {
        var error = new Error("Test.Error", "Something failed.");
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessT_ReturnsValue()
    {
        var result = Result.Success("Sample");

        Assert.True(result.IsSuccess);
        Assert.Equal("Sample", result.Value);
    }

    [Fact]
    public void FailureT_ThrowsOnValueAccess()
    {
        var error = new Error("Test.Error", "Failed");
        var result = Result.Failure<string>(error);

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void TryGetValue_WhenSuccessful_ReturnsTrueAndValue()
    {
        var result = Result.Success(42);

        var success = result.TryGetValue(out var value);

        Assert.True(success);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_WhenFailed_ReturnsFalseAndDefault()
    {
        var result = Result.Failure<int>(new Error("Fail", "Failed"));

        var success = result.TryGetValue(out var value);

        Assert.False(success);
        Assert.Equal(0, value);
    }
}
