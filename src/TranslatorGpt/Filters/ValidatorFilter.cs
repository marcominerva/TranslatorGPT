using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OperationResults.AspNetCore.Http;

namespace TranslatorGpt.Filters;

public class ValidatorFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> validator;
    private readonly OperationResultOptions options;

    public ValidatorFilter(IValidator<T> validator, OperationResultOptions options)
    {
        this.validator = validator;
        this.options = options;
    }

    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.FirstOrDefault(a => a.GetType() == typeof(T)) is T input)
        {
            var validationResult = await validator.ValidateAsync(input);
            if (!validationResult.IsValid)
            {
                var statusCode = StatusCodes.Status400BadRequest;
                var problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Type = $"https://httpstatuses.io/{statusCode}",
                    Title = "One or more validation errors occurred",
                    Instance = context.HttpContext.Request.Path
                };

                problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                if (options.ErrorResponseFormat == ErrorResponseFormat.Default)
                {
                    problemDetails.Extensions["errors"] = validationResult.ToDictionary();
                }
                else
                {
                    var errors = validationResult.Errors.Select(e => new { Name = e.PropertyName, Message = e.ErrorMessage });
                    problemDetails.Extensions["errors"] = errors;
                }

                return TypedResults.Json(problemDetails, statusCode: StatusCodes.Status400BadRequest, contentType: "application/problem+json; charset=utf-8");
            }

            return await next(context);
        }

        return TypedResults.BadRequest();
    }
}

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class
        => builder.AddEndpointFilter<ValidatorFilter<T>>();
}