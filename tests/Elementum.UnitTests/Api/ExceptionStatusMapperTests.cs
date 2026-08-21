using Elementum.Api.Exceptions;
using Elementum.Application.Exceptions;
using Elementum.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Elementum.UnitTests.Api;

public class ExceptionStatusMapperTests
{
    // [R]IGHT-BICEP: domain invariant violations are semantic 422 client errors with unprocessable-entity metadata
    [Fact]
    public void Map_WhenDomainInvariantViolated_Returns422WithUnprocessableEntityTypeUri()
    {
        // Arrange
        var exception = DomainValidationException.NullOrWhitespace("symbol");

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(422, mapping.StatusCode);
        Assert.Equal("Domain validation error", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc4918#section-11.2", mapping.TypeUri);
    }

    // [R]IGHT-BICEP: ArgumentOutOfRangeException arm maps invalid numeric input to 400 bad request
    [Fact]
    public void Map_WhenArgumentOutOfRange_Returns400WithBadRequestTypeUri()
    {
        // Arrange
        var exception = InvalidPriceException.MustBePositive("price", 0m);

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
        Assert.Equal("Invalid request", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", mapping.TypeUri);
    }

    // [R]IGHT-BICEP: unsupported currency is a domain invariant and maps to 422 unprocessable entity
    [Fact]
    public void Map_WhenUnsupportedCurrency_Returns422WithUnprocessableEntityTypeUri()
    {
        // Arrange
        var exception = UnsupportedCurrencyException.ForCode("GBP");

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(422, mapping.StatusCode);
        Assert.Equal("Domain validation error", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc4918#section-11.2", mapping.TypeUri);
    }

    // [R]IGHT-BICEP: missing resources map to 404 not found with RFC 7231 section 6.5.4 type URI
    [Fact]
    public void Map_WhenResourceNotFound_Returns404WithNotFoundTypeUri()
    {
        // Arrange
        var exception = new KeyNotFoundException("metal symbol");

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, mapping.StatusCode);
        Assert.Equal("Resource not found", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", mapping.TypeUri);
    }

    // [R]IGHT-BICEP: empty upstream live quote maps to 502 bad gateway for the primary live-price failure path
    [Fact]
    public void Map_WhenExternalApiReturnsEmpty_Returns502WithBadGatewayTypeUri()
    {
        // Arrange
        var exception = ExternalApiException.EmptyLiveQuoteResponse();

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status502BadGateway, mapping.StatusCode);
        Assert.Equal("Upstream service error", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.3", mapping.TypeUri);
    }

    // [R]IGHT-BICEP: HTTP transport failures map to 502 so clients can distinguish upstream from internal errors
    [Fact]
    public void Map_WhenHttpRequestFails_Returns502WithBadGatewayTypeUri()
    {
        // Arrange
        var exception = new HttpRequestException("connection refused");

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status502BadGateway, mapping.StatusCode);
        Assert.Equal("Upstream service error", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.3", mapping.TypeUri);
    }

    // [R]IGHT-BICEP: timeout exceptions map to 504 gateway timeout with RFC 7231 section 6.6.5 type URI
    [Fact]
    public void Map_WhenTimeoutOccurs_Returns504WithGatewayTimeoutTypeUri()
    {
        // Arrange
        var exception = new TimeoutException();

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status504GatewayTimeout, mapping.StatusCode);
        Assert.Equal("Gateway timeout", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.5", mapping.TypeUri);
    }

    // [B]OUNDARY RIGHT-BICEP: client cancellation uses non-RFC status 499 and intentionally omits a type URI
    [Fact]
    public void Map_WhenClientCancelled_Returns499WithoutTypeUri()
    {
        // Arrange
        var exception = new OperationCanceledException();

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(499, mapping.StatusCode);
        Assert.Equal("Request cancelled", mapping.Title);
        Assert.Null(mapping.TypeUri);
    }

    // [E]RROR RIGHT-BICEP: unmapped exceptions must fall through to 500 default, not client-error statuses
    [Fact]
    public void Map_WhenExceptionIsUnknown_Returns500WithInternalErrorMetadata()
    {
        // Arrange
        var exception = new Exception("unexpected failure");

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, mapping.StatusCode);
        Assert.Equal("An error occurred", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.1", mapping.TypeUri);
    }

    // [E]RROR RIGHT-BICEP: ConfigurationException must map to 500 before the generic ApplicationException 502 arm
    [Fact]
    public void Map_WhenConfigurationMissing_Returns500Not502()
    {
        // Arrange
        var exception = ConfigurationException.MissingConnectionString("DefaultConnection");

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, mapping.StatusCode);
        Assert.Equal("An error occurred", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.1", mapping.TypeUri);
    }

    // [E]RROR RIGHT-BICEP: generic ApplicationException subclasses (non-configuration) map to 502 upstream error
    [Fact]
    public void Map_WhenGenericApplicationException_Returns502WithBadGatewayTypeUri()
    {
        // Arrange
        var exception = new TestUpstreamApplicationException();

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(StatusCodes.Status502BadGateway, mapping.StatusCode);
        Assert.Equal("Upstream service error", mapping.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.6.3", mapping.TypeUri);
    }

    // [E]RROR RIGHT-BICEP: TaskCanceledException must match its dedicated switch arm before OperationCanceledException
    [Fact]
    public void Map_WhenTaskCancelled_Returns499ViaTaskCanceledArm()
    {
        // Arrange
        var exception = new TaskCanceledException();

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(499, mapping.StatusCode);
        Assert.Equal("Request cancelled", mapping.Title);
        Assert.Null(mapping.TypeUri);
    }

    // [C]ROSS-CHECK RIGHT-BICEP: 400 bad-request type URI matches ValidationFilter RFC 7231 section 6.5.1 reference
    [Fact]
    public void Map_WhenArgumentException_TypeUriMatchesValidationFilterBadRequestReference()
    {
        // Arrange
        const string validationFilterBadRequestType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        var exception = new ArgumentException("malformed input");

        // Act
        var mapping = ExceptionStatusMapper.Map(exception);

        // Assert
        Assert.Equal(validationFilterBadRequestType, mapping.TypeUri);
        Assert.Equal(StatusCodes.Status400BadRequest, mapping.StatusCode);
    }

    private sealed class TestUpstreamApplicationException : Elementum.Application.Exceptions.ApplicationException
    {
        public TestUpstreamApplicationException() : base("generic upstream failure") { }
    }
}
