using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Elementum.Api.Filters;

/// <summary>
/// Action filter that automatically runs registered FluentValidation validators for incoming action arguments
/// and produces RFC 7807 ValidationProblemDetails on validation failures.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (paramName, paramValue) in context.ActionArguments)
        {
            if (paramValue is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(paramValue.GetType());
            if (_serviceProvider.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(paramValue);
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());

                    var problemDetails = new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "One or more validation errors occurred.",
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        Instance = context.HttpContext.Request.Path
                    };

                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }
            }
        }

        await next();
    }
}
