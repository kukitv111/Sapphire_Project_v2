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
        => result.IsSuccess ? onSuccess(result.Value) : Failure(result.Error);

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
