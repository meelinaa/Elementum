using Elementum.Domain.Common;
using Elementum.Domain.Exceptions;

namespace Elementum.Domain.Tests.Common;

public class ResultTests
{
    // [R]IGHT-BICEP: Verifies that creating a parameterless success result marks IsSuccess as true
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    // [R]IGHT-BICEP: Verifies that creating a failed result marks IsFailure as true and sets error
    [Fact]
    public void Failure_CreatesFailedResult_WithError()
    {
        // Arrange
        var error = new Error("Test.Error", "Something failed.");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    // [R]IGHT-BICEP: Verifies that typed Success carries and exposes its encapsulated value
    [Fact]
    public void SuccessT_ReturnsValue()
    {
        // Arrange
        const string payload = "Sample";

        // Act
        var result = Result.Success(payload);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(payload, result.Value);
    }

    // [E]RROR: Verifies that accessing .Value on a failed Result<T> throws ResultException
    [Fact]
    public void FailureT_ThrowsOnValueAccess()
    {
        // Arrange
        var error = new Error("Test.Error", "Failed");
        var result = Result.Failure<string>(error);

        // Act & Assert
        Assert.True(result.IsFailure);
        Assert.Throws<ResultException>(() => _ = result.Value);
    }

    // [E]RROR: Verifies that creating a failure result with Error.None throws ResultException
    [Fact]
    public void Failure_WithErrorNone_ThrowsResultException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ResultException>(() => Result.Failure(Error.None));
    }

    // [R]IGHT-BICEP: Verifies that TryGetValue on success returns true and extracts value
    [Fact]
    public void TryGetValue_WhenSuccessful_ReturnsTrueAndValue()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var success = result.TryGetValue(out var value);

        // Assert
        Assert.True(success);
        Assert.Equal(42, value);
    }

    // [B]OUNDARY: Verifies that TryGetValue on failure returns false and emits default(T)
    [Fact]
    public void TryGetValue_WhenFailed_ReturnsFalseAndDefault()
    {
        // Arrange
        var result = Result.Failure<int>(new Error("Fail", "Failed"));

        // Act
        var success = result.TryGetValue(out var value);

        // Assert
        Assert.False(success);
        Assert.Equal(0, value);
    }
}
