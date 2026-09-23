using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Shared.Security;

// Keep the existing response envelope while exposing failures through HTTP status codes.
public sealed class ResultStatusFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context) { }
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult { Value: Result { IsFailure: true } result } response)
            response.StatusCode = result.Error.Type switch
            {
                ErrorType.Validation => 400,
                ErrorType.Unauthorized => 401,
                ErrorType.Forbidden => 403,
                ErrorType.NotFound => 404,
                ErrorType.Conflict => 409,
                _ => 500
            };
    }
}
