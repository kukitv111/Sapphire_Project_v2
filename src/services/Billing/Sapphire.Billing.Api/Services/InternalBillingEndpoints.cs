using System.Security.Cryptography;
using System.Text;
using Sapphire.Billing.Infrastructure.Services;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Billing.Api.Services;

public static class InternalBillingEndpoints
{
    public static void MapInternalBillingEndpoints(this WebApplication app)
    {
        app.MapPost("/internal/reservations", async (HttpContext http, ReserveRequest request,
            BillingReservationService billing, IConfiguration config, CancellationToken ct) =>
        {
            if (!Authorized(http, config)) return Results.Unauthorized();
            var result = await billing.ReserveAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value)
                : Results.Problem(statusCode: Status(result.Error), title: result.Error.Code,
                    detail: result.Error.Description);
        }).AllowAnonymous();

        app.MapPost("/internal/reservations/{sessionId:guid}/release", async (HttpContext http,
            Guid sessionId, BillingReservationService billing, IConfiguration config, CancellationToken ct) =>
        {
            if (!Authorized(http, config)) return Results.Unauthorized();
            var result = await billing.ReleaseAsync(sessionId, ct);
            return result.IsSuccess ? Results.Ok()
                : Results.Problem(statusCode: Status(result.Error), title: result.Error.Code);
        }).AllowAnonymous();
    }

    private static bool Authorized(HttpContext http, IConfiguration config)
    {
        var expected = config["Messaging:SharedSecret"];
        var supplied = http.Request.Headers["X-Sapphire-Message-Key"].ToString();
        return !string.IsNullOrWhiteSpace(expected) && supplied.Length == expected.Length &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected));
    }

    private static int Status(Error error) => error.Type switch
    {
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.Validation => 400,
        _ => 500
    };
}
