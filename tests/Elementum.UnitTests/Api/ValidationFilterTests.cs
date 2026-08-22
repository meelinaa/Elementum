using Elementum.Api.Filters;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Elementum.UnitTests.Api;

public class ValidationFilterTests
{
    // [R]IGHT-BICEP: valid models pass through the filter and invoke the next delegate
    [Fact]
    public async Task OnActionExecutionAsync_WhenModelIsValid_InvokesNext()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestValidationModel>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IValidator<TestValidationModel>)))
            .Returns(validatorMock.Object);

        var filter = new ValidationFilter(serviceProviderMock.Object);
        var context = CreateActionExecutingContext(new Dictionary<string, object?>
        {
            ["request"] = new TestValidationModel { Name = "Valid" }
        });

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    // RIGHT-BIC[E]P: validation failure short-circuits execution with HTTP 400 ValidationProblemDetails
    [Fact]
    public async Task OnActionExecutionAsync_WhenValidationFails_ReturnsBadRequestProblemDetails()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Symbol", "The Symbol field is required.")
        };
        var validatorMock = new Mock<IValidator<TestValidationModel>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IValidator<TestValidationModel>)))
            .Returns(validatorMock.Object);

        var filter = new ValidationFilter(serviceProviderMock.Object);
        var context = CreateActionExecutingContext(new Dictionary<string, object?>
        {
            ["request"] = new TestValidationModel()
        });

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.False(nextCalled);
        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("One or more validation errors occurred.", problem.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problem.Type);
        Assert.True(problem.Errors.ContainsKey("Symbol"));
        Assert.Equal("The Symbol field is required.", problem.Errors["Symbol"][0]);
    }

    // RIGHT-[B]ICEP: null arguments are skipped gracefully without throwing NullReferenceException
    [Fact]
    public async Task OnActionExecutionAsync_WhenArgumentIsNull_InvokesNextWithoutError()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var filter = new ValidationFilter(serviceProviderMock.Object);
        var context = CreateActionExecutingContext(new Dictionary<string, object?>
        {
            ["request"] = null
        });

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    // RIGHT-[B]ICEP: arguments without registered validators pass through to next delegate
    [Fact]
    public async Task OnActionExecutionAsync_WhenNoValidatorRegistered_InvokesNext()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(It.IsAny<Type>())).Returns((object?)null);

        var filter = new ValidationFilter(serviceProviderMock.Object);
        var context = CreateActionExecutingContext(new Dictionary<string, object?>
        {
            ["unvalidatedParam"] = new object()
        });

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    private static ActionExecutingContext CreateActionExecutingContext(IDictionary<string, object?> actionArguments)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/test";
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments,
            new object());
    }
}

public class TestValidationModel
{
    public string Name { get; set; } = string.Empty;
}
