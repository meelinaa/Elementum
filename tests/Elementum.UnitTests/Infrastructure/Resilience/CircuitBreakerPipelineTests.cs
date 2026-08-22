using Polly;
using Polly.CircuitBreaker;

namespace Elementum.UnitTests.Infrastructure.Resilience;

public class CircuitBreakerPipelineTests
{
    // RIGHT-BIC[E]P: consecutive failures exceeding throughput threshold trip circuit to Open and throw BrokenCircuitException
    [Fact]
    public async Task ExecuteAsync_WhenConsecutiveFailuresExceedThreshold_TripsCircuitToOpen()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromMilliseconds(500),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();

        // Act & Assert - 2 consecutive failures to trip the circuit
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            pipeline.ExecuteAsync(_ => ValueTask.FromException(new HttpRequestException("Failure 1"))).AsTask());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            pipeline.ExecuteAsync(_ => ValueTask.FromException(new HttpRequestException("Failure 2"))).AsTask());

        // Circuit is now OPEN - subsequent call is short-circuited immediately with BrokenCircuitException
        var ex = await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            pipeline.ExecuteAsync(_ => ValueTask.FromResult("Should not execute")).AsTask());

        Assert.NotNull(ex);
    }

    // RIGHT-BICE[P]: after break duration expires, circuit transitions to Half-Open and resets to Closed on successful execution
    [Fact]
    public async Task ExecuteAsync_AfterBreakDurationExpires_ClosesCircuitOnSuccessfulCall()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromMilliseconds(500),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();

        // Trip the circuit to OPEN
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            pipeline.ExecuteAsync(_ => ValueTask.FromException(new HttpRequestException("Failure 1"))).AsTask());
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            pipeline.ExecuteAsync(_ => ValueTask.FromException(new HttpRequestException("Failure 2"))).AsTask());

        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            pipeline.ExecuteAsync(_ => ValueTask.FromResult("Short-circuited")).AsTask());

        // Wait for break duration to expire (transition from OPEN -> HALF-OPEN)
        await Task.Delay(600);

        // Act - execute successful call in Half-Open state
        var result = await pipeline.ExecuteAsync(_ => ValueTask.FromResult("Recovered"));

        // Assert - circuit is now Closed again and returns result
        Assert.Equal("Recovered", result);
    }
}
