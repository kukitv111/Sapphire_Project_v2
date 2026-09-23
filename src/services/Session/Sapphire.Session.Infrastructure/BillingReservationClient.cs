using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Sapphire.Session.Application;
using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Session.Infrastructure;

public sealed class BillingReservationClient(HttpClient http, IConfiguration config) : IBillingReservationClient
{
    public async Task<Result> ReserveAsync(Guid sessionId, Guid userId, Guid entitlementId,
        DateTime startedAt, DateTime plannedEndAt, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/reservations")
        {
            Content = JsonContent.Create(new { sessionId, userId, entitlementId, startedAt, plannedEndAt })
        };
        request.Headers.Add("X-Sapphire-Message-Key", config["Messaging:SharedSecret"]);
        using var response = await http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return Result.Success();
        if ((int)response.StatusCode == 409) return Result.Failure(Error.Conflict("Insufficient prepaid balance or concurrent reservation"));
        if ((int)response.StatusCode == 404) return Result.Failure(Error.NotFound("Entitlement not found"));
        throw new HttpRequestException($"Billing reserve failed: {(int)response.StatusCode}");
    }

    public async Task<Result> ReleaseAsync(Guid sessionId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/reservations/{sessionId}/release");
        request.Headers.Add("X-Sapphire-Message-Key", config["Messaging:SharedSecret"]);
        using var response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode ? Result.Success()
            : Result.Failure(Error.Conflict("Could not release reservation"));
    }
}
