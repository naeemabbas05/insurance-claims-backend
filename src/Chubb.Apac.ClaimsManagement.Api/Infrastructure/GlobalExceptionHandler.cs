using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            PolicyNotFoundException => StatusCodes.Status404NotFound,
            ClaimLifecycleNotFoundException => StatusCodes.Status404NotFound,
            IdempotencyKeyConflictException => StatusCodes.Status409Conflict,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            ClaimLifecycleAccessDeniedException or ClaimLifecycleActorNotFoundException =>
                StatusCodes.Status403Forbidden,
            DomainAuthorizationException => StatusCodes.Status403Forbidden,
            MarketNotConfiguredException or DomainRuleViolationException =>
                StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }
        else
        {
            logger.LogWarning(
                "Request rejected with status {StatusCode}: {ExceptionType}",
                status,
                exception.GetType().Name);
        }

        ProblemDetails problemDetails;
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());
            problemDetails = new ValidationProblemDetails(errors)
            {
                Status = status,
                Title = "One or more validation errors occurred.",
                Instance = httpContext.Request.Path
            };
        }
        else
        {
            problemDetails = new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    StatusCodes.Status404NotFound => "The requested resource was not found.",
                    StatusCodes.Status409Conflict => exception is IdempotencyKeyConflictException
                        ? "The idempotency key conflicts with an earlier request."
                        : "The resource was changed by another request.",
                    StatusCodes.Status403Forbidden => "The current actor is not authorized for this claim.",
                    StatusCodes.Status422UnprocessableEntity => "The claim submission is not valid.",
                    _ => "An unexpected error occurred."
                },
                Detail = status == StatusCodes.Status422UnprocessableEntity ? exception.Message : null,
                Instance = httpContext.Request.Path
            };
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
