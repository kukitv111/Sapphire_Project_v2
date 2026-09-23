using Microsoft.AspNetCore.Mvc;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Auth.Api.Controllers;

/// <summary>
/// Base controller that maps Result/Error outcomes to HTTP responses.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Maps a non-generic Result to an HTTP response (NoContent on success).
    /// </summary>
    protected IActionResult FromResult(Result result)
        => result.IsSuccess ? NoContent() : Failure(result.Error);

    /// <summary>
    /// Maps a generic Result to an HTTP response using the provided success projection.
    /// </summary>
    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (!result.IsSuccess)
            return Failure(result.Error);

        return result.Value is { } value
            ? onSuccess(value)
            : Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "invalid_result",
                detail: "A successful result must contain a value.");
    }

    private IActionResult Failure(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: error.Code, detail: error.Description);
    }
}
