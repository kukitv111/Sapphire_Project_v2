using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sapphire.Shared.Kernel.Exceptions;

namespace Sapphire.Auth.Api.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions into RFC 7807 ProblemDetails responses.
/// Domain exceptions are mapped to HTTP status codes via their business error codes.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, code, detail) = exception switch
        {
            DomainException domain => MapDomainException(domain),
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "validation_failed",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                "You are not authorized to perform this action"),
            ArgumentException argument => (
                StatusCodes.Status400BadRequest,
                "bad_request",
                argument.Message),
            BadHttpRequestException badRequest => (
                StatusCodes.Status400BadRequest,
                "bad_request",
                badRequest.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "An unexpected error occurred")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = code,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Code, string Detail) MapDomainException(DomainException exception)
    {
        var status = exception.Code switch
        {
            "USER_NOT_FOUND" or "ROLE_NOT_FOUND" or "PERMISSION_NOT_FOUND" => StatusCodes.Status404NotFound,
            "USERNAME_TAKEN" or "EMAIL_REGISTERED" or "PHONE_REGISTERED" => StatusCodes.Status409Conflict,
            "INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "USER_BANNED" or "USER_SUSPENDED" or "INSUFFICIENT_PERMISSIONS" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return (status, exception.Code, exception.Message);
    }
}
